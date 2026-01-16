namespace ExpertEase.Application.DataTransferObjects.UserDTOs;

public record FacebookUserResponse
{
    public string Id { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string Email { get; init; } = null!;
    public FacebookPicture? Picture { get; init; }
}
