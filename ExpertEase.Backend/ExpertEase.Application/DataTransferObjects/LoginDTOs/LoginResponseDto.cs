using System.ComponentModel.DataAnnotations;
using ExpertEase.Application.DataTransferObjects.UserDTOs;

namespace ExpertEase.Application.DataTransferObjects.LoginDTOs;

/// <summary>
/// This DTO is used to respond to a login with the JWT token and user information.
/// </summary>
public record LoginResponseDto
{
    public string Token { get; init; } = null!;
    public UserDto User { get; init; } = null!;
}
