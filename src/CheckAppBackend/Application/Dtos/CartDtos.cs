namespace CheckAppBackend.Application.Dtos;

public record AddToCartDto(Guid ProductId, int Quantity = 1);

public record UpdateCartItemDto(int Quantity);

public record CartItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    string ProductImageUrl,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal,
    int StockAvailable
);

public record CartResponseDto(
    List<CartItemDto> Items,
    decimal Subtotal,
    decimal EstimatedTax,
    decimal ShippingFee,
    decimal TotalAmount
);
