namespace ExpertEase.Application.DataTransferObjects.ReviewDTOs;

public record ReviewDto
{
    public string SenderUserFullName { get; init; } = null!;
    public string? SenderUserProfilePictureUrl { get; init; }
    public int Rating { get; init; }
    public string Content { get; init; } = null!;
}
