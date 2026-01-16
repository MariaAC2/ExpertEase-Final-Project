using ExpertEase.Application.DataTransferObjects.CategoryDTOs;

namespace ExpertEase.Application.DataTransferObjects.SpecialistProfileDTOs;

public record SpecialistProfileDto
{
    public int YearsExperience { get; init; }
    public string Description { get; init; } = string.Empty;
    public List<CategoryDto> Categories { get; init; } = new List<CategoryDto>();
    public List<string?>? PortfolioPhotos { get; init; }
    public string? StripeAccountId { get; init; }
}