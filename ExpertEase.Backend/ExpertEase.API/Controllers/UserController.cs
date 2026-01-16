using ExpertEase.Application.DataTransferObjects.UserDTOs;
using ExpertEase.Application.Requests;
using ExpertEase.Application.Responses;
using ExpertEase.Application.Services;
using ExpertEase.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpertEase.API.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class UserController(IUserService userService) : AuthorizedController(userService)
{
    [Authorize(Roles = "Admin")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RequestResponse<UserDto>>> GetById([FromRoute] Guid id)
    {
        var currentUser = await GetCurrentUser();

        return currentUser.Result != null ?
            CreateRequestResponseFromServiceResponse(await UserService.GetUserAdmin(id, currentUser.Result.Id)) :
            CreateErrorMessageResult<UserDto>(currentUser.Error);
    }
    
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<RequestResponse<UserProfileDto>>> GetProfile()
    {
        var currentUser = await GetCurrentUser();

        return currentUser.Result != null ?
            CreateRequestResponseFromServiceResponse(await UserService.GetUserProfile(currentUser.Result.Id)) :
            CreateErrorMessageResult<UserProfileDto>(currentUser.Error);
    }
    
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RequestResponse<UserDetailsDto>>> GetDetails([FromRoute] Guid id)
    {
        return CreateRequestResponseFromServiceResponse(await UserService.GetUserDetails(id));
    }
    
    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RequestResponse<UserPaymentDetailsDto>>> GetPaymentDetails([FromRoute] Guid id)
    {
        var currentUser = await GetCurrentUser();
        
        return currentUser.Result != null ?
            CreateRequestResponseFromServiceResponse(await UserService.GetUserPaymentDetails(id)) :
            CreateErrorMessageResult<UserPaymentDetailsDto>(currentUser.Error);
    }
    
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<RequestResponse<PagedResponse<UserDto>>>> GetPage([FromQuery] PaginationSearchQueryParams pagination)
    {
        var currentUser = await GetCurrentUser();

        return currentUser.Result != null ?
            CreateRequestResponseFromServiceResponse(await UserService.GetUsers(currentUser.Result.Id, pagination)) :
            CreateErrorMessageResult<PagedResponse<UserDto>>(currentUser.Error);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<RequestResponse>> Add([FromBody] UserAddDto user)
    {
        var currentUser = await GetCurrentUser();
        var newUser = user with
        {
            Password = PasswordUtils.HashPassword(user.Password)
        };

        return currentUser.Result != null ?
            CreateRequestResponseFromServiceResponse(await UserService.AddUser(newUser, currentUser.Result)) :
            CreateErrorMessageResult(currentUser.Error);
    }
    
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<RequestResponse>> Update([FromBody] AdminUserUpdateDto user)
    {
        var currentUser = await GetCurrentUser();

        var response = await UserService.AdminUpdateUser(user, currentUser.Result);

        return CreateRequestResponseFromServiceResponse(response);
    }
    
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<RequestResponse>> Delete([FromRoute] Guid id)
    {
        var currentUser = await GetCurrentUser();

        return currentUser.Result != null ?
            CreateRequestResponseFromServiceResponse(await UserService.DeleteUser(id)) :
            CreateErrorMessageResult(currentUser.Error);
    }
    
    [Authorize]
    [HttpPatch]
    public async Task<ActionResult<RequestResponse<UserUpdateResponseDto>>> Update([FromBody] UserUpdateDto user)
    {
        var currentUser = await GetCurrentUser();

        return currentUser.Result != null ?
            CreateRequestResponseFromServiceResponse(await UserService.UpdateUser(user with
            {
                Password = !string.IsNullOrWhiteSpace(user.Password) ? PasswordUtils.HashPassword(user.Password) : null
            }, currentUser.Result)) :
            CreateErrorMessageResult<UserUpdateResponseDto>(currentUser.Error);
    }
}