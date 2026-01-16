namespace ExpertEase.Application.DataTransferObjects.PaymentDTOs;

public record PaymentConfirmationDto
{
    public string PaymentIntentId { get; init; } = null!;
    public Guid ReplyId { get; init; }
    public decimal ServiceAmount { get; init; }
    public decimal ProtectionFee { get; init; }
    public decimal TotalAmount { get; init; }
    public string PaymentMethod { get; init; } = null!;
}