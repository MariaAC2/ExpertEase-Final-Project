namespace ExpertEase.Application.DataTransferObjects.SpecialistProfileDTOs;

public record SpecialistProfileUpdateDto
{
    public Guid UserId { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Address { get; init; }
    public int? YearsExperience { get; init; }
    public string? Description { get; init; }
    public List<Guid>? CategoryIds { get; init; }
    public List<string>? ExistingPortfolioPhotoUrls { get; init; }
    public List<string>? PhotoIdsToRemove { get; init; }
}