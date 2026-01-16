namespace ExpertEase.Application.DataTransferObjects.PaymentDTOs;

public record PaymentAddDto
{
    public Guid ReplyId { get; init; }
    public decimal ServiceAmount { get; init; }
    public decimal ProtectionFee { get; init; }
    public string StripeAccountId { get; init; } = null!;
}