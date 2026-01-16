
namespace ExpertEase.Application.DataTransferObjects.CategoryDTOs;

public record CategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
}