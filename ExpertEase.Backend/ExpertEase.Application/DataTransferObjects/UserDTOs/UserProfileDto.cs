namespace ExpertEase.Application.DataTransferObjects.UserDTOs;

public record UserProfileDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = null!;
    public string? ProfilePictureUrl { get; init; }
    public int Rating { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    // Specialist-only fields (null if client)
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Address { get; init; }
    public int? YearsExperience { get; init; }
    public string? Description { get; init; }
    public string? StripeAccountId { get; init; }
    public List<string?>? Portfolio { get; init; }
    public List<string>? Categories { get; init; }
}
