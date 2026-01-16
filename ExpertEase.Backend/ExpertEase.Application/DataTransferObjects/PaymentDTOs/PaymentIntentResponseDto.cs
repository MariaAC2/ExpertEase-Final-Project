using ExpertEase.Application.DataTransferObjects.ProtectionFeeDTOs;

namespace ExpertEase.Application.DataTransferObjects.PaymentDTOs;

public record PaymentIntentResponseDto
{
    public string ClientSecret { get; init; } = null!;
    public string PaymentIntentId { get; init; } = null!;
    public string StripeAccountId { get; init; } = null!;
    public decimal ServiceAmount { get; init; }
    public decimal ProtectionFee { get; init; }
    public decimal TotalAmount { get; init; }
    public ProtectionFeeDetailsDto? ProtectionFeeDetails { get; init; }
}