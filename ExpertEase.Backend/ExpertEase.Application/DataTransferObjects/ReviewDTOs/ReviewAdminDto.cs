namespace ExpertEase.Application.DataTransferObjects.ReviewDTOs;

public record ReviewAdminDto
{
    public Guid Id { get; init; }
    public Guid SenderUserId { get; init; }
    public Guid ReceiverUserId { get; init; }
    public Guid ServiceTaskId { get; init; }
    public string Content { get; init; } = null!;
    public int Rating { get; init; }
}