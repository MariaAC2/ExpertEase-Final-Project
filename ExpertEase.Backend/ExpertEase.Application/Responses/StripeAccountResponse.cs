namespace ExpertEase.Application.Responses;

public class StripeAccountResponse
{
    public string AccountId { get; set; } = string.Empty;
    public string OnboardingUrl { get; set; } = string.Empty;
}