namespace ExpertEase.Application.DataTransferObjects.StripeAccountDTOs;
public record StripePaymentIntentAddDto
{
    public decimal TotalAmount { get; init; }
    public decimal ServiceAmount { get; init; }
    public decimal ProtectionFee { get; init; }
    public string SpecialistAccountId { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Currency { get; init; } = "ron";
    public Dictionary<string, string> Metadata { get; init; } = new();
}