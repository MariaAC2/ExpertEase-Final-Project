namespace ExpertEase.Application.DataTransferObjects.UserDTOs;

public record SocialUserInfo
{
    public string Email { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Picture { get; init; }
}
