namespace ExpertEase.Application.DataTransferObjects.StripeAccountDTOs;

public record StripeAccountLinkResponseDto
{
    public string Url { get; init; } = null!;
}