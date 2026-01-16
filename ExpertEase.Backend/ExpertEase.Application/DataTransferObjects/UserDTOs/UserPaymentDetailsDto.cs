using System;

namespace ExpertEase.Application.DataTransferObjects.UserDTOs;

public record UserPaymentDetailsDto
{
    public Guid UserId { get; init; }
    public string UserFullName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? PhoneNumber { get; init; }
}
