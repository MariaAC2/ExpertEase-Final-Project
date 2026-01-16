using ExpertEase.Application.DataTransferObjects.ProtectionFeeDTOs;

namespace ExpertEase.Application.DataTransferObjects.PaymentDTOs;

public record PaymentIntentAddDto
{
    public Guid ReplyId { get; init; }
    public decimal ServiceAmount { get; init; }
    public decimal ProtectionFee { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "ron";
    public string? Description { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
    public ProtectionFeeDetailsDto? ProtectionFeeDetails { get; init; }
}