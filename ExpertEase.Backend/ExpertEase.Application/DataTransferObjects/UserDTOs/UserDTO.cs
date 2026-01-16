using ExpertEase.Domain.Enums;

namespace ExpertEase.Application.DataTransferObjects.UserDTOs;

/// <summary>
/// This DTO is used to transfer information about a user within the application and to client application.
/// Note that it doesn't contain a password property and that is why you should use DTO rather than entities to use only the data that you need or protect sensible information.
/// </summary>
public record UserDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public UserRoleEnum Role { get; init; }
    public string ProfilePictureUrl { get; init; } = null!;
    public AuthProvider AuthProvider { get; init; } = AuthProvider.Local;
    public string? StripeCustomerId { get; init; } = null!;
}
