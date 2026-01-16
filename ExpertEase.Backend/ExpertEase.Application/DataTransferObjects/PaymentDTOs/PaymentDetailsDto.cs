using ExpertEase.Application.DataTransferObjects.ProtectionFeeDTOs;

namespace ExpertEase.Application.DataTransferObjects.PaymentDTOs;

public record PaymentDetailsDto
{
    public Guid Id { get; init; }
    public Guid ReplyId { get; init; }
    public decimal ServiceAmount { get; init; }
    public decimal ProtectionFee { get; init; }
    public decimal TotalAmount { get; init; }
    
    public string Currency { get; init; } = null!;
    public string Status { get; init; } = null!;
    public DateTime? PaidAt { get; init; }
    public DateTime? EscrowReleasedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    
    public string? StripePaymentIntentId { get; init; }
    public string? StripeTransferId { get; init; }
    public string? StripeRefundId { get; init; }
    
    public string ServiceDescription { get; init; } = null!;
    public string ServiceAddress { get; init; } = null!;
    public DateTime ServiceStartDate { get; init; }
    public DateTime ServiceEndDate { get; init; }
    public string SpecialistName { get; init; } = null!;
    public string ClientName { get; init; } = null!;
    
    public decimal TransferredAmount { get; init; }
    public decimal RefundedAmount { get; init; }
    public decimal PlatformRevenue { get; init; }
    public bool IsEscrowed { get; init; }
    public ProtectionFeeDetailsDto? ProtectionFeeDetails { get; init; }
}