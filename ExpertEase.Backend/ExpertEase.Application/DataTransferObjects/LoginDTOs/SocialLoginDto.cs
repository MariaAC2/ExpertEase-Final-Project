namespace ExpertEase.Application.DataTransferObjects.LoginDTOs;

public record SocialLoginDto
{
    public string Provider { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
}