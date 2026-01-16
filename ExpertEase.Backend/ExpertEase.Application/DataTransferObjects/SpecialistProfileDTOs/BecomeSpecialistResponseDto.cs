using ExpertEase.Application.DataTransferObjects.UserDTOs;

namespace ExpertEase.Application.DataTransferObjects.SpecialistProfileDTOs;

public record BecomeSpecialistResponseDto
{
    public string Token { get; init; } = null!;
    public UserDto User { get; init; } = null!;
    public string StripeAccountId { get; init; } = null!;
}