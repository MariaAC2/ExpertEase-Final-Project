namespace ExpertEase.Application.DataTransferObjects.StripeAccountDTOs;

public record StripeAccountStatusDto
{
    public string AccountId { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool ChargesEnabled { get; init; }
    public bool PayoutsEnabled { get; init; }
    public bool DetailsSubmitted { get; init; }
    public List<string> RequirementsCurrentlyDue { get; init; } = new();
    public List<string> RequirementsEventuallyDue { get; init; } = new();
    public string? DisabledReason { get; init; }
    public bool IsTestMode { get; init; }
    public bool CanReceivePayments { get; init; }
}