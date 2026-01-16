using ExpertEase.Application.DataTransferObjects.CategoryDTOs;

namespace ExpertEase.Application.DataTransferObjects.SpecialistDTOs;

public record SpecialistDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? ProfilePictureUrl { get; init; } = null!;
    public string PhoneNumber { get; init; } = null!;
    public string Address { get; init; } = null!;
    public int YearsExperience { get; init; }
    public string Description { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int Rating { get; init; } = 0;
    public List<CategoryDto> Categories { get; init; } = null!;
}