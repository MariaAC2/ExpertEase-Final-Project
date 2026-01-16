using System;
using ExpertEase.Application.DataTransferObjects.SpecialistProfileDTOs;
using ExpertEase.Domain.Enums;

namespace ExpertEase.Application.DataTransferObjects.UserDTOs;

public record UserAdminDetailsDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public UserRoleEnum Role { get; init; }
    public AuthProvider AuthProvider { get; init; } = AuthProvider.Local;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int Rating { get; init; } = 0;
    public string? ProfilePictureUrl { get; init; }
    public ContactInfoDto? ContactInfo { get; init; }
    public SpecialistProfileDto? Specialist { get; init; }
}
