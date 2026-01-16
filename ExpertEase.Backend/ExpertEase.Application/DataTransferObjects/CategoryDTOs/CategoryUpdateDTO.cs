namespace ExpertEase.Application.DataTransferObjects.CategoryDTOs;

public record CategoryUpdateDto
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
}