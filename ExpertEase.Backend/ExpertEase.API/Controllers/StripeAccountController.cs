using System.Net;
using System.Runtime.InteropServices.JavaScript;
using ExpertEase.Application.DataTransferObjects;
using ExpertEase.Application.DataTransferObjects.StripeAccountDTOs;
using ExpertEase.Application.Errors;
using ExpertEase.Application.Responses;
using ExpertEase.Application.Services;
using ExpertEase.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpertEase.API.Controllers;

[ApiController]
[Route("api/stripe/account")]
public class StripeAccountController(IUserService userService, 
    IStripeAccountService stripeService,
    ISpecialistProfileService specialistProfileService) : AuthorizedController(userService)
{
    [Authorize]
    [HttpPost("onboarding-link/{accountId}")]
    public async Task<ActionResult<RequestResponse<StripeAccountLinkResponseDto>>> CreateLink([FromRoute] string accountId)
    {
        var currentUser = await GetCurrentUser();
        return currentUser.Result != null
            ? CreateRequestResponseFromServiceResponse(await stripeService.GenerateOnboardingLink(accountId))
            : CreateErrorMessageResult<StripeAccountLinkResponseDto>();
    }
    
    [Authorize]
    [HttpPost("dashboard-link/{accountId}")]
    public async Task<ActionResult<RequestResponse<StripeAccountLinkResponseDto>>> CreateDashboardLink([FromRoute] string accountId)
    {
        var currentUser = await GetCurrentUser();
        return currentUser.Result != null
            ? CreateRequestResponseFromServiceResponse(await stripeService.GenerateDashboardLink(accountId))
            : CreateErrorMessageResult<StripeAccountLinkResponseDto>();
    }
    
    [Authorize]
    [HttpGet("status/{accountId}")]
    public async Task<ActionResult<RequestResponse<StripeAccountStatusDto>>> GetAccountStatus([FromRoute] string accountId)
    {
        var currentUser = await GetCurrentUser();
        return currentUser.Result != null
            ? CreateRequestResponseFromServiceResponse(await stripeService.GetAccountStatus(accountId))
            : CreateErrorMessageResult<StripeAccountStatusDto>();
    }
    
    
// Add this to your StripeAccountController.cs
    [Authorize(Roles = "Admin")] // Only admins can do this
    [HttpPost("admin/generate-onboarding/{userId}")]
    public async Task<ActionResult<RequestResponse<StripeAccountLinkResponseDto>>> GenerateOnboardingForUser([FromRoute] Guid userId)
    {
        try
        {
            var currentUser = await GetCurrentUser();
            // Get the user's Stripe account ID
            var user = await specialistProfileService.GetSpecialistProfile(userId);
            if (user.Result.StripeAccountId == null)
            {
                return CreateErrorMessageResult<StripeAccountLinkResponseDto>(new ErrorMessage(HttpStatusCode.BadRequest, "No stripe account found"));
            }

            var stripeAccountId = user.Result.StripeAccountId;
        
            // Generate onboarding link
            var result = await stripeService.GenerateOnboardingLink(stripeAccountId);
        
            return CreateRequestResponseFromServiceResponse(result);
        }
        catch (Exception ex)
        {
            return CreateErrorMessageResult<StripeAccountLinkResponseDto>(new ErrorMessage(HttpStatusCode.InternalServerError, $"Error: {ex.Message}"));
        }
    }
}
