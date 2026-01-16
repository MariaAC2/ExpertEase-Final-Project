using ExpertEase.Application.DataTransferObjects;
using ExpertEase.Application.DataTransferObjects.UserDTOs;

namespace ExpertEase.Application.Services;

/// <summary>
/// This service is used to emit a JWT token.
/// </summary>
public interface ILoginService
{
    /// <summary>
    /// GetToken returns a JWT token string for a user with an issue date and and expiration interval after issue.
    /// </summary>
    public string GetToken(UserDto user, DateTime issuedAt, TimeSpan expiresIn);
}
