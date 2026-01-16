namespace ExpertEase.Application.DataTransferObjects.ProtectionFeeDTOs;

public record PaymentAmountBreakdown
{
    public decimal ServiceAmount { get; init; }
    public decimal ProtectionFee { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal TransferredAmount { get; init; }
    public decimal RefundedAmount { get; init; }
    public decimal PendingAmount { get; init; }
    public decimal PlatformRevenue { get; init; }
    public ProtectionFeeCalculation? ProtectionFeeCalculation { get; init; }
}