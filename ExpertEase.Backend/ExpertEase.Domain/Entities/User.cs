using ExpertEase.Domain.Enums;

namespace ExpertEase.Domain.Entities;

/// <summary>
/// This is an example for a user entity, it will be mapped to a single table and each property will have it's own column except for entity object references also known as navigation properties.
/// </summary>
public class User : BaseEntity
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public UserRoleEnum Role { get; set; }
    public AuthProvider AuthProvider { get; set; } = AuthProvider.Local;
    public string? ProfilePictureUrl { get; set; }
    public int Rating = 0;
    public string StripeCustomerId { get; set; } = null!;
    public ContactInfo? ContactInfo { get; set; }
    public SpecialistProfile? SpecialistProfile { get; set; }
}
