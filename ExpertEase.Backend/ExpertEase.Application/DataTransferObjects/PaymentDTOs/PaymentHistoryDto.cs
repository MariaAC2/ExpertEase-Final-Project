namespace ExpertEase.Application.DataTransferObjects.PaymentDTOs;

public record PaymentHistoryDto
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
    public string ServiceDescription { get; init; } = null!;
    public string ServiceAddress { get; init; } = null!;
    public string SpecialistName { get; init; } = null!;
    public string ClientName { get; init; } = null!;
    public decimal TransferredAmount { get; init; }
    public decimal RefundedAmount { get; init; }
    public bool IsEscrowed { get; init; }
}