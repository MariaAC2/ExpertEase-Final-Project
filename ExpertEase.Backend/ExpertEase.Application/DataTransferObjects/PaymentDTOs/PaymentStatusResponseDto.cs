using ExpertEase.Application.DataTransferObjects.ProtectionFeeDTOs;

namespace ExpertEase.Application.DataTransferObjects.PaymentDTOs;

public record PaymentStatusResponseDto
{
    public Guid PaymentId { get; init; }
    public Guid? ServiceTaskId { get; init; }
    public string Status { get; init; } = null!;
    public bool IsEscrowed { get; init; }
    public bool CanBeReleased { get; init; }
    public bool CanBeRefunded { get; init; }
    public PaymentAmountBreakdown AmountBreakdown { get; init; } = new();
    public ProtectionFeeDetailsDto? ProtectionFeeDetails { get; init; }
}