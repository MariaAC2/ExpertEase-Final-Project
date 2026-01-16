using System.Security.Claims;
using ExpertEase.Application.DataTransferObjects;
using ExpertEase.Application.DataTransferObjects.UserDTOs;
using ExpertEase.Application.Responses;
using ExpertEase.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ExpertEase.Infrastructure.Authorization;

/// <summary>
/// This abstract class is used as a base class for controllers that need to get current information about the user from the database.
/// </summary>
public abstract class AuthorizedController(IUserService userService) : ResponseController
{
    private UserClaims? _userClaims;
    protected readonly IUserService UserService = userService;

    private UserClaims ExtractClaims()
    {
        if (_userClaims != null)
        {
            return _userClaims;
        }

        var enumerable = User.Claims.ToList();
        var userId = enumerable.Where(x => x.Type == ClaimTypes.NameIdentifier).Select(x => Guid.Parse(x.Value)).FirstOrDefault();
        var email = enumerable.Where(x => x.Type == ClaimTypes.Email).Select(x => x.Value).FirstOrDefault();
        var name = enumerable.Where(x => x.Type == ClaimTypes.Name).Select(x => x.Value).FirstOrDefault();
        var role = enumerable.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value).FirstOrDefault();

        _userClaims = new(userId, name, email, role);

        return _userClaims;
    }
    
    protected Task<ServiceResponse<UserDto>> GetCurrentUser() => UserService.GetUser(ExtractClaims().Id);
}
