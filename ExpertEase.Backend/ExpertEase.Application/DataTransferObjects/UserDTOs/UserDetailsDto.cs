using System;
using System.Collections.Generic;
using ExpertEase.Application.DataTransferObjects.ReviewDTOs;

namespace ExpertEase.Application.DataTransferObjects.UserDTOs;

public record UserDetailsDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = null!;

    public string? ProfilePictureUrl { get; init; }

    public int Rating { get; init; }

    public List<ReviewDto> Reviews { get; init; } = new List<ReviewDto>();

    // Specialist-only fields (null if client)
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Address { get; init; }
    public int? YearsExperience { get; init; }
    public string? Description { get; init; }
    public List<string?>? Portfolio { get; init; }
    public List<string>? Categories { get; init; }
}
