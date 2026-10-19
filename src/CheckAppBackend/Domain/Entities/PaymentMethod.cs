namespace CheckAppBackend.Domain.Entities;

public class PaymentMethod
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string CardHolderName { get; set; } = string.Empty;
    public string CardNumberMasked { get; set; } = string.Empty;
    public string ExpiryMonth { get; set; } = string.Empty;
    public string ExpiryYear { get; set; } = string.Empty;
    public string Brand { get; set; } = "Visa";
    public bool IsDefault { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
