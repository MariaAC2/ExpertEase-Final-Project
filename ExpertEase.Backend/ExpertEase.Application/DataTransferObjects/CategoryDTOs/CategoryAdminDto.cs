namespace ExpertEase.Application.DataTransferObjects.CategoryDTOs;

public record CategoryAdminDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public int SpecialistsCount { get; init; }
    public List<Guid> SpecialistIds { get; init; } = null!;
}