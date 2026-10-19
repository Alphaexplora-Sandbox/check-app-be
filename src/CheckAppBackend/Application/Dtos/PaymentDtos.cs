namespace CheckAppBackend.Application.Dtos;

public record CreatePaymentMethodDto(
    string CardHolderName,
    string CardNumber,
    string ExpiryMonth,
    string ExpiryYear,
    string? Brand = "Visa",
    bool IsDefault = false
);

public record PaymentMethodResponseDto(
    Guid Id,
    Guid UserId,
    string CardHolderName,
    string CardNumberMasked,
    string ExpiryMonth,
    string ExpiryYear,
    string Brand,
    bool IsDefault,
    DateTime CreatedAt
);
