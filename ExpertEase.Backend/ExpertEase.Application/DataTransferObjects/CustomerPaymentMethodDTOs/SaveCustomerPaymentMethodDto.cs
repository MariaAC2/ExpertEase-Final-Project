namespace ExpertEase.Application.DataTransferObjects.CustomerPaymentMethodDTOs;

public record SaveCustomerPaymentMethodDto
{
    public string PaymentMethodId { get; init; } = string.Empty;
    public string CardLast4 { get; init; } = string.Empty;
    public string CardBrand { get; init; } = string.Empty;
    public string CardholderName { get; init; } = string.Empty;
    public bool IsDefault { get; init; } = true;
}
