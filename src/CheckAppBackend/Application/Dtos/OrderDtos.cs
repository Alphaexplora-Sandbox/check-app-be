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
    string? PaymentMethod = "credit_card",
    Guid? SavedPaymentMethodId = null,
    bool SaveCardForFutureUse = false
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

public record CancelOrderRequestDto(
    string? Reason = "Customer requested cancellation"
);

public record OrderMilestoneDto(
    string Title,
    string Description,
    DateTime Timestamp,
    bool Completed,
    bool Current
);

public record OrderTrackingResponseDto(
    Guid OrderId,
    string OrderNumber,
    string Status,
    DateTime OrderDate,
    string ShippingAddress,
    List<OrderMilestoneDto> Milestones
);
