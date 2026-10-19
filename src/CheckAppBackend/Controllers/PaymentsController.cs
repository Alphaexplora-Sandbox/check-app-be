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
[Route("api/v1/payments")]
public class PaymentsController : ControllerBase
{
    private readonly EcommerceMockDbContext _dbContext;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(EcommerceMockDbContext dbContext, ILogger<PaymentsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet("methods")]
    public async Task<ActionResult<IEnumerable<PaymentMethodResponseDto>>> GetPaymentMethods()
    {
        var userId = User.GetUserId();
        var methods = await _dbContext.PaymentMethods
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.IsDefault)
            .ThenByDescending(p => p.CreatedAt)
            .Select(p => MapToDto(p))
            .ToListAsync();

        return Ok(methods);
    }

    [HttpPost("methods")]
    public async Task<ActionResult<PaymentMethodResponseDto>> CreatePaymentMethod([FromBody] CreatePaymentMethodDto request)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrWhiteSpace(request.CardHolderName))
        {
            return BadRequest(new { message = "Card holder name is required." });
        }
        var digits = new string(request.CardNumber?.Where(char.IsDigit).ToArray() ?? Array.Empty<char>());
        if (digits.Length < 13 || digits.Length > 19)
        {
            return BadRequest(new { message = "Invalid card number format." });
        }
        var masked = $"•••• •••• •••• {digits[^4..]}";
        var brand = string.IsNullOrWhiteSpace(request.Brand)
            ? (digits.StartsWith("4") ? "Visa" : digits.StartsWith("5") ? "Mastercard" : "Card")
            : request.Brand;
        if (request.IsDefault)
        {
            var existingDefaults = await _dbContext.PaymentMethods
                .Where(p => p.UserId == userId && p.IsDefault)
                .ToListAsync();
            foreach (var m in existingDefaults)
            {
                m.IsDefault = false;
            }
        }
        else
        {
            var hasCards = await _dbContext.PaymentMethods.AnyAsync(p => p.UserId == userId);
            if (!hasCards)
            {
                request = request with { IsDefault = true };
            }
        }
        var method = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CardHolderName = request.CardHolderName.Trim(),
            CardNumberMasked = masked,
            ExpiryMonth = request.ExpiryMonth.PadLeft(2, '0'),
            ExpiryYear = request.ExpiryYear,
            Brand = brand,
            IsDefault = request.IsDefault,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.PaymentMethods.Add(method);
        await _dbContext.SaveChangesAsync();
        return CreatedAtAction(nameof(GetPaymentMethods), new { id = method.Id }, MapToDto(method));
    }

    [HttpDelete("methods/{id:guid}")]
    public async Task<IActionResult> DeletePaymentMethod(Guid id)
    {
        var userId = User.GetUserId();
        var method = await _dbContext.PaymentMethods
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        if (method == null)
        {
            return NotFound(new { message = "Payment method not found." });
        }
        _dbContext.PaymentMethods.Remove(method);
        if (method.IsDefault)
        {
            var next = await _dbContext.PaymentMethods
                .Where(p => p.UserId == userId && p.Id != id)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
            if (next != null)
            {
                next.IsDefault = true;
            }
        }
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("methods/{id:guid}/default")]
    public async Task<ActionResult<PaymentMethodResponseDto>> SetDefaultPaymentMethod(Guid id)
    {
        var userId = User.GetUserId();
        var target = await _dbContext.PaymentMethods
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        if (target == null)
        {
            return NotFound(new { message = "Payment method not found." });
        }
        var allMethods = await _dbContext.PaymentMethods
            .Where(p => p.UserId == userId)
            .ToListAsync();
        foreach (var m in allMethods)
        {
            m.IsDefault = (m.Id == id);
        }
        await _dbContext.SaveChangesAsync();
        return Ok(MapToDto(target));
    }

    private static PaymentMethodResponseDto MapToDto(PaymentMethod p) =>
        new(p.Id, p.UserId, p.CardHolderName, p.CardNumberMasked, p.ExpiryMonth, p.ExpiryYear, p.Brand, p.IsDefault, p.CreatedAt);
}
