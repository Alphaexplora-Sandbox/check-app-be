namespace CheckAppBackend.Application.Dtos;

public record ShippingAddressDto(
    string FullName,
    string StreetAddress,
    string City,
    string PostalCode,
    string? Country = "United States"
);

public record CheckoutRequestDto(
    ShippingAddressDto ShippingAddress,
    string? PaymentMethod = "credit_card"
);

public record OrderItemDto(
    Guid Id,
    Guid ProductId,
    string? ProductName,
    string? ProductImageUrl,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal
);

public record OrderResponseDto(
    Guid Id,
    Guid UserId,
    string OrderNumber,
    decimal TotalAmount,
    string Status,
    ShippingAddressDto? ShippingAddress,
    string PaymentMethod,
    DateTime CreatedAt,
    List<OrderItemDto> OrderItems
);

public record UpdateOrderStatusDto(string Status);
