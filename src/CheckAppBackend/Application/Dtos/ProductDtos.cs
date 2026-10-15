namespace CheckAppBackend.Application.Dtos;

public record CategoryDto(Guid Id, string Name, string Slug, string? Description, string? ImageUrl);

public record ProductDto(
    Guid Id,
    Guid CategoryId,
    string? CategoryName,
    string? CategorySlug,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    int StockQuantity,
    string ImageUrl,
    bool IsFeatured,
    decimal Rating
);

public record CreateProductDto(
    Guid CategoryId,
    string Name,
    string? Slug,
    string Description,
    decimal Price,
    int StockQuantity,
    string ImageUrl,
    bool IsFeatured
);

public record UpdateStockDto(int StockQuantity);

public class ProductFilterParams
{
    public string? Search { get; set; }
    public string? Category { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? SortBy { get; set; } // "price_asc", "price_desc", "featured", "rating", "newest"
    public bool? Featured { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
