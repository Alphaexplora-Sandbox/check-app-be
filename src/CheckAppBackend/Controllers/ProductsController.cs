using CheckAppBackend.Application.Dtos;
using CheckAppBackend.Domain.Entities;
using CheckAppBackend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CheckAppBackend.Controllers;

[ApiController]
[Route("api/v1/products")]
public class ProductsController : ControllerBase
{
    private readonly EcommerceMockDbContext _dbContext;

    public ProductsController(EcommerceMockDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts([FromQuery] ProductFilterParams filter)
    {
        var query = _dbContext.Products
            .Include(p => p.Category)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(search) || p.Description.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            var category = filter.Category.Trim().ToLower();
            query = query.Where(p => p.Category != null && (p.Category.Slug.ToLower() == category || p.Category.Name.ToLower() == category));
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(p => p.Price >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= filter.MaxPrice.Value);
        }

        if (filter.Featured.HasValue && filter.Featured.Value)
        {
            query = query.Where(p => p.IsFeatured);
        }

        query = filter.SortBy?.ToLowerInvariant() switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "rating" => query.OrderByDescending(p => p.Rating),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.IsFeatured).ThenBy(p => p.Name)
        };

        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var products = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductDto(
                p.Id,
                p.CategoryId,
                p.Category != null ? p.Category.Name : null,
                p.Category != null ? p.Category.Slug : null,
                p.Name,
                p.Slug,
                p.Description,
                p.Price,
                p.StockQuantity,
                p.ImageUrl,
                p.IsFeatured,
                p.Rating
            ))
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetProductById(Guid id)
    {
        var p = await _dbContext.Products
            .Include(x => x.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (p == null)
        {
            return NotFound(new { message = $"Product with ID {id} was not found." });
        }

        return Ok(new ProductDto(
            p.Id,
            p.CategoryId,
            p.Category?.Name,
            p.Category?.Slug,
            p.Name,
            p.Slug,
            p.Description,
            p.Price,
            p.StockQuantity,
            p.ImageUrl,
            p.IsFeatured,
            p.Rating
        ));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto request)
    {
        var category = await _dbContext.Categories.FindAsync(request.CategoryId);
        if (category == null)
        {
            return BadRequest(new { message = "Invalid category ID." });
        }

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? request.Name.ToLowerInvariant().Replace(' ', '-').Replace("/", "")
            : request.Slug;

        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = request.CategoryId,
            Name = request.Name,
            Slug = slug,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            ImageUrl = request.ImageUrl,
            IsFeatured = request.IsFeatured,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, new ProductDto(
            product.Id,
            product.CategoryId,
            category.Name,
            category.Slug,
            product.Name,
            product.Slug,
            product.Description,
            product.Price,
            product.StockQuantity,
            product.ImageUrl,
            product.IsFeatured,
            product.Rating
        ));
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:guid}/stock")]
    public async Task<ActionResult<ProductDto>> UpdateStock(Guid id, [FromBody] UpdateStockDto request)
    {
        var product = await _dbContext.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            return NotFound(new { message = "Product not found." });
        }

        if (request.StockQuantity < 0)
        {
            return BadRequest(new { message = "Stock quantity cannot be negative." });
        }

        product.StockQuantity = request.StockQuantity;
        product.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return Ok(new ProductDto(
            product.Id,
            product.CategoryId,
            product.Category?.Name,
            product.Category?.Slug,
            product.Name,
            product.Slug,
            product.Description,
            product.Price,
            product.StockQuantity,
            product.ImageUrl,
            product.IsFeatured,
            product.Rating
        ));
    }
}
