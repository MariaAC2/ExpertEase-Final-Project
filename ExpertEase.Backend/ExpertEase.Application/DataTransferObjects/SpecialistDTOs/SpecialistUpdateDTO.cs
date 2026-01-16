namespace ExpertEase.Application.DataTransferObjects.SpecialistDTOs;

public record SpecialistUpdateDto
{
    public Guid Id { get; init; }
    public string? FullName { get; init; } = null!;
    public string? PhoneNumber { get; init; } = null!;
    public string? Address { get; init; } = null!;
    public int? YearsExperience { get; init; }
    public string? Description { get; init; } = null!;
}