namespace ExpertEase.Application.DataTransferObjects.UserDTOs;

public record OAuthCodeExchangeDto
{
    public string Code { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string RedirectUri { get; init; } = string.Empty;
}
