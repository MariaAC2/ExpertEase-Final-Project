using Microsoft.AspNetCore.Http;

namespace ExpertEase.Application.DataTransferObjects.SpecialistProfileDTOs;

public record SpecialistProfileUpdateFormDto
{
    public Guid UserId { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Address { get; init; }
    public int? YearsExperience { get; init; }
    public string? Description { get; init; }
    public Guid[]? CategoryIds { get; init; }
    public IFormFile[]? NewPortfolioPhotos { get; init; }
    public string[]? ExistingPortfolioPhotoUrls { get; init; }
    public string[]? PhotoIdsToRemove { get; init; }
}