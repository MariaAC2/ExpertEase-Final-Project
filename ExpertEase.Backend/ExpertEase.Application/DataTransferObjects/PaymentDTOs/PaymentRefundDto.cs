namespace ExpertEase.Application.DataTransferObjects.PaymentDTOs;

public record PaymentRefundDto
{
    public Guid PaymentId { get; init; }
    public decimal? Amount { get; init; }
    public string? Reason { get; init; }
    
    public bool RefundServiceAmount { get; init; } = true;
    public bool RefundProtectionFee { get; init; } = true;
}