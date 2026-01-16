
namespace ExpertEase.Application.DataTransferObjects.ReviewDTOs;

public record ReviewAddDto
{
    public Guid ReceiverUserId { get; init; }
    public string Content { get; init; } = null!;
    public int Rating { get; init; }
}