using CheckAppBackend.Application.Dtos;
using CheckAppBackend.Common;
using CheckAppBackend.Domain.Entities;
using CheckAppBackend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CheckAppBackend.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/cart")]
public class CartController : ControllerBase
{
    private readonly EcommerceMockDbContext _dbContext;
    private const decimal TaxRate = 0.085m;
    private const decimal FreeShippingThreshold = 150.00m;
    private const decimal StandardShippingFee = 15.00m;

    public CartController(EcommerceMockDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<CartResponseDto>> GetCart()
    {
        var userId = User.GetUserId();
        var cartItems = await _dbContext.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.UpdatedAt)
            .ToListAsync();

        var itemDtos = cartItems.Select(c => new CartItemDto(
            c.Id,
            c.ProductId,
            c.Product?.Name ?? "Unknown Product",
            c.Product?.Slug ?? "",
            c.Product?.ImageUrl ?? "",
            c.Product?.Price ?? 0m,
            c.Quantity,
            (c.Product?.Price ?? 0m) * c.Quantity,
            c.Product?.StockQuantity ?? 0
        )).ToList();

        var subtotal = itemDtos.Sum(i => i.Subtotal);
        var estimatedTax = Math.Round(subtotal * TaxRate, 2);
        var shippingFee = subtotal >= FreeShippingThreshold || subtotal == 0m ? 0.00m : StandardShippingFee;
        var totalAmount = subtotal + estimatedTax + shippingFee;

        return Ok(new CartResponseDto(itemDtos, subtotal, estimatedTax, shippingFee, totalAmount));
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartResponseDto>> AddItem([FromBody] AddToCartDto request)
    {
        var userId = User.GetUserId();
        var product = await _dbContext.Products.FindAsync(request.ProductId);

        if (product == null)
        {
            return NotFound(new { message = "Product not found." });
        }

        var quantityToAdd = Math.Max(1, request.Quantity);

        var existingItem = await _dbContext.CartItems
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == request.ProductId);

        if (existingItem != null)
        {
            var newQuantity = existingItem.Quantity + quantityToAdd;
            if (newQuantity > product.StockQuantity)
            {
                return BadRequest(new { message = $"Cannot add {quantityToAdd} more. Total requested exceeds available stock of {product.StockQuantity}." });
            }
            existingItem.Quantity = newQuantity;
            existingItem.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            if (quantityToAdd > product.StockQuantity)
            {
                return BadRequest(new { message = $"Cannot add {quantityToAdd}. Available stock is {product.StockQuantity}." });
            }

            var newItem = new CartItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProductId = request.ProductId,
                Quantity = quantityToAdd,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.CartItems.Add(newItem);
        }

        await _dbContext.SaveChangesAsync();
        return await GetCart();
    }

    [HttpPut("items/{id:guid}")]
    public async Task<ActionResult<CartResponseDto>> UpdateItem(Guid id, [FromBody] UpdateCartItemDto request)
    {
        var userId = User.GetUserId();
        var item = await _dbContext.CartItems
            .Include(c => c.Product)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (item == null)
        {
            return NotFound(new { message = "Cart item not found." });
        }

        if (request.Quantity <= 0)
        {
            _dbContext.CartItems.Remove(item);
        }
        else
        {
            if (item.Product != null && request.Quantity > item.Product.StockQuantity)
            {
                return BadRequest(new { message = $"Requested quantity exceeds available stock of {item.Product.StockQuantity}." });
            }
            item.Quantity = request.Quantity;
            item.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
        return await GetCart();
    }

    [HttpDelete("items/{id:guid}")]
    public async Task<ActionResult<CartResponseDto>> DeleteItem(Guid id)
    {
        var userId = User.GetUserId();
        var item = await _dbContext.CartItems
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (item != null)
        {
            _dbContext.CartItems.Remove(item);
            await _dbContext.SaveChangesAsync();
        }

        return await GetCart();
    }

    [HttpDelete]
    public async Task<ActionResult> ClearCart()
    {
        var userId = User.GetUserId();
        var items = await _dbContext.CartItems.Where(c => c.UserId == userId).ToListAsync();
        _dbContext.CartItems.RemoveRange(items);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
