using Microsoft.AspNetCore.Http;

namespace ExpertEase.Application.DataTransferObjects.SpecialistProfileDTOs;

public record BecomeSpecialistFormDto
{
    public Guid UserId { get; init; }
    public string PhoneNumber { get; init; } = null!;
    public string Address { get; init; } = null!;
    public int YearsExperience { get; init; }
    public string Description { get; init; } = null!;
    public List<Guid>? Categories { get; init; }
    public List<IFormFile>? PortfolioPhotos { get; init; }
}