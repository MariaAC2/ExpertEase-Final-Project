
namespace ExpertEase.Application.DataTransferObjects.ReviewDTOs;

public record ReviewUpdateDto
{
    public Guid Id { get; init; }
    public string? Content { get; init; } = null!;
    public int? Rating { get; init; }
}