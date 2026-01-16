using ExpertEase.Application.DataTransferObjects.PhotoDTOs;

namespace ExpertEase.Application.DataTransferObjects.SpecialistProfileDTOs;

public record BecomeSpecialistDto
{
    public Guid UserId { get; init; }
    public string PhoneNumber { get; init; } = null!;
    public string Address { get; init; } = null!;
    public int YearsExperience { get; init; }
    public string Description { get; init; } = null!;
    public List<Guid>? Categories { get; init; }
    public List<PortfolioPictureAddDto>? PortfolioPhotos { get; init; }
}
