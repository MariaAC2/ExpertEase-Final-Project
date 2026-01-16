using ExpertEase.Domain.Enums;

namespace ExpertEase.Application.DataTransferObjects.PaymentDTOs;

public record PaymentReportItemDto
{
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public decimal ServiceAmount { get; init; }
    public decimal ProtectionFee { get; init; }
    public decimal TotalAmount { get; init; }
    public PaymentStatusEnum Status { get; init; }
    public decimal TransferredAmount { get; init; }
    public decimal RefundedAmount { get; init; }
    public bool FeeCollected { get; init; }
    public DateTime? PaidAt { get; init; }
    public DateTime? EscrowReleasedAt { get; init; }
    public bool IsEscrowed { get; init; }
}