using System.Security.Claims;
using System.Text.Json;
using CheckAppBackend.Application.Dtos;
using CheckAppBackend.Common;
using CheckAppBackend.Domain.Entities;
using CheckAppBackend.Domain.Enums;
using CheckAppBackend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CheckAppBackend.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/orders")]
public class OrdersController : ControllerBase
{
    private readonly EcommerceMockDbContext _dbContext;
    private readonly ILogger<OrdersController> _logger;
    private const decimal TaxRate = 0.085m;
    private const decimal FreeShippingThreshold = 150.00m;
    private const decimal StandardShippingFee = 15.00m;

    public OrdersController(EcommerceMockDbContext dbContext, ILogger<OrdersController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpPost("checkout")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<OrderResponseDto>> Checkout([FromBody] CheckoutRequestDto request)
    {
        var userId = User.GetUserId();

        if (request.ShippingAddress == null ||
            string.IsNullOrWhiteSpace(request.ShippingAddress.FullName) ||
            string.IsNullOrWhiteSpace(request.ShippingAddress.StreetAddress) ||
            string.IsNullOrWhiteSpace(request.ShippingAddress.City) ||
            string.IsNullOrWhiteSpace(request.ShippingAddress.PostalCode))
        {
            return BadRequest(new { message = "Complete shipping address is required." });
        }

        var isInMemory = _dbContext.Database.ProviderName?.Contains("InMemory") == true;
        var transaction = isInMemory ? null : await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var cartItems = await _dbContext.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                return BadRequest(new { message = "Cart is empty." });
            }

            decimal subtotal = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in cartItems)
            {
                if (item.Product == null)
                {
                    return BadRequest(new { message = "Cart contains an invalid product." });
                }

                if (item.Product.StockQuantity < item.Quantity)
                {
                    return Conflict(new
                    {
                        message = $"Insufficient stock for {item.Product.Name}. Available: {item.Product.StockQuantity}"
                    });
                }

                // Deduct stock count atomically
                item.Product.StockQuantity -= item.Quantity;
                item.Product.UpdatedAt = DateTime.UtcNow;

                var lineTotal = item.Product.Price * item.Quantity;
                subtotal += lineTotal;

                orderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = item.ProductId,
                    UnitPrice = item.Product.Price,
                    Quantity = item.Quantity,
                    Subtotal = lineTotal,
                    CreatedAt = DateTime.UtcNow
                });
            }

            var estimatedTax = Math.Round(subtotal * TaxRate, 2);
            var shippingFee = subtotal >= FreeShippingThreshold ? 0.00m : StandardShippingFee;
            var totalAmount = subtotal + estimatedTax + shippingFee;

            var orderNumber = $"ECM-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}";

            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrderNumber = orderNumber,
                TotalAmount = totalAmount,
                Status = OrderStatus.Processing,
                ShippingAddress = JsonSerializer.Serialize(request.ShippingAddress),
                PaymentMethod = request.PaymentMethod ?? "credit_card",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                OrderItems = orderItems
            };

            _dbContext.Orders.Add(order);
            _dbContext.CartItems.RemoveRange(cartItems);

            await _dbContext.SaveChangesAsync();

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            var responseDto = MapToResponseDto(order, request.ShippingAddress);
            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, responseDto);
        }
        catch (Exception ex)
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            _logger.LogError(ex, "Checkout transaction failed for user {UserId}", userId);
            return StatusCode(500, new { message = "Order processing encountered an internal error." });
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetOrders()
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsInRole("Admin");

        var query = _dbContext.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .AsNoTracking();

        if (!isAdmin)
        {
            query = query.Where(o => o.UserId == userId);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var result = orders.Select(o => MapToResponseDto(o)).ToList();
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponseDto>> GetOrderById(Guid id)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsInRole("Admin");

        var order = await _dbContext.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound(new { message = "Order not found." });
        }

        if (!isAdmin && order.UserId != userId)
        {
            return Forbid();
        }

        return Ok(MapToResponseDto(order));
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponseDto>> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusDto request)
    {
        var order = await _dbContext.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound(new { message = "Order not found." });
        }

        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var newStatus))
        {
            return BadRequest(new { message = $"Invalid order status '{request.Status}'." });
        }

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Ok(MapToResponseDto(order));
    }

    private static OrderResponseDto MapToResponseDto(Order order, ShippingAddressDto? address = null)
    {
        if (address == null && !string.IsNullOrEmpty(order.ShippingAddress))
        {
            try
            {
                address = JsonSerializer.Deserialize<ShippingAddressDto>(order.ShippingAddress);
            }
            catch
            {
                // Fallback if parsing fails
            }
        }

        var items = order.OrderItems.Select(oi => new OrderItemDto(
            oi.Id,
            oi.ProductId,
            oi.Product?.Name,
            oi.Product?.ImageUrl,
            oi.UnitPrice,
            oi.Quantity,
            oi.Subtotal
        )).ToList();

        return new OrderResponseDto(
            order.Id,
            order.UserId,
            order.OrderNumber,
            order.TotalAmount,
            order.Status.ToString(),
            address,
            order.PaymentMethod,
            order.CreatedAt,
            items
        );
    }
}
