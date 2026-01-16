namespace ExpertEase.Application.DataTransferObjects.PaymentDTOs;
public record PaymentReleaseDto
{
    public Guid PaymentId { get; init; }
    public string? Reason { get; init; } = "Service completed successfully";
    public decimal? CustomAmount { get; init; } // If null, release full service amount
}