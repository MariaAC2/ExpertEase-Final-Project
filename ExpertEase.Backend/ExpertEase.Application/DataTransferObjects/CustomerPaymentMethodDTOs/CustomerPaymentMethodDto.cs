namespace ExpertEase.Application.DataTransferObjects.CustomerPaymentMethodDTOs;

public record CustomerPaymentMethodDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string StripeCustomerId { get; init; } = string.Empty;
    public string StripePaymentMethodId { get; init; } = string.Empty;
    public string CardLast4 { get; init; } = string.Empty;
    public string CardBrand { get; init; } = string.Empty;
    public string CardholderName { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
    public DateTime CreatedAt { get; init; }
}