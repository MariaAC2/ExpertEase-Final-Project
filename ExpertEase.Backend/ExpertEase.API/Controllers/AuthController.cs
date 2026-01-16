using System.Net;
using ExpertEase.Application.DataTransferObjects.LoginDTOs;
using ExpertEase.Application.DataTransferObjects.UserDTOs;
using ExpertEase.Application.Errors;
using ExpertEase.Application.Responses;
using ExpertEase.Application.Services;
using ExpertEase.Domain.Enums;
using ExpertEase.Infrastructure.Authorization;
using ExpertEase.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpertEase.API.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class AuthController(IUserService userService) : ResponseController
{
    [HttpPost]
    public async Task<ActionResult<RequestResponse<LoginResponseDto>>> Login([FromBody] LoginDto login) // The FromBody attribute indicates that the parameter is deserialized from the JSON body.
    {
        return CreateRequestResponseFromServiceResponse(await userService.Login(login with { Password = PasswordUtils.HashPassword(login.Password)})); // The "with" keyword works only with records and it creates another object instance with the updated properties. 
    }
    
    [HttpPost]
    public async Task<ActionResult<RequestResponse<LoginResponseDto>>> SocialLogin([FromBody] SocialLoginDto login) // The FromBody attribute indicates that the parameter is deserialized from the JSON body.
    {
        return CreateRequestResponseFromServiceResponse(await userService.SocialLogin(login));
    }
    
    [HttpPost]
    public async Task<ActionResult<RequestResponse>> Register([FromBody] UserRegisterDto regDto)
    {
        var role = regDto.Email.EndsWith("@admin.com", StringComparison.OrdinalIgnoreCase)
            ? UserRoleEnum.Admin
            : UserRoleEnum.Client;
        
        var user = new UserAddDto
        {
            FullName = regDto.FirstName + " " + regDto.LastName,
            Email = regDto.Email,
            Password = PasswordUtils.HashPassword(regDto.Password),
            Role = role
        };
        
        return CreateRequestResponseFromServiceResponse(await userService.AddUser(user));
    }
    
    [HttpPost]
    public async Task<ActionResult<RequestResponse<LoginResponseDto>>> ExchangeOAuthCode([FromBody] OAuthCodeExchangeDto exchangeDto, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await userService.ExchangeOAuthCode(exchangeDto, cancellationToken);
            
            if (result.IsSuccess)
            {
                return CreateRequestResponseFromServiceResponse(result);
            }
            
            return CreateRequestResponseFromServiceResponse(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OAuth exchange controller error: {ex.Message}");
            
            var errorResponse = ServiceResponse.CreateErrorResponse<LoginResponseDto>(
                new ErrorMessage(HttpStatusCode.InternalServerError, "OAuth exchange failed", ErrorCodes.Invalid));
            
            return CreateRequestResponseFromServiceResponse(errorResponse);
        }
    }
}
