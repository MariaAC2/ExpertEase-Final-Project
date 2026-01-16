namespace ExpertEase.Application.DataTransferObjects.PaymentDTOs;

public record PaymentReportDto
{
    public string Period { get; init; } = string.Empty;
    
    // ✅ Revenue Breakdown
    public decimal TotalServiceRevenue { get; init; }
    public decimal TotalProtectionFees { get; init; }
    public decimal TotalPlatformRevenue { get; init; }
    
    // ✅ Transaction Statistics  
    public int TotalTransactions { get; init; }
    public int CompletedServices { get; init; }
    public int RefundedServices { get; init; }
    public int EscrowedPayments { get; init; }
    
    // ✅ Business Metrics
    public decimal RefundRate { get; init; }
    public decimal AverageServiceValue { get; init; }
    public decimal AverageProtectionFee { get; init; }
    public decimal TotalEscrowedAmount { get; init; }
}