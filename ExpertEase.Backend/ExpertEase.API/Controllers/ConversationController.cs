using System.Diagnostics;
using ExpertEase.Application.DataTransferObjects.FirestoreDTOs;
using ExpertEase.Application.DataTransferObjects.UserDTOs;
using ExpertEase.Application.Requests;
using ExpertEase.Application.Responses;
using ExpertEase.Application.Services;
using ExpertEase.Infrastructure.Authorization;
using ExpertEase.Infrastructure.Firestore.FirestoreDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpertEase.API.Controllers;

[ApiController]
[Route("/api/[controller]/[action]")]
public class ConversationController(IUserService userService, IConversationService conversationService): AuthorizedController(userService)
{
    [Authorize]
    [HttpGet("{senderId:guid}")]
    public async Task<ActionResult<RequestResponse<PagedResponse<ConversationItemDto>>>> GetById([FromQuery]PaginationQueryParams pagination, [FromRoute] Guid senderId)
    {
        var currentUser = await GetCurrentUser();
        return currentUser.Result != null ? 
            CreateRequestResponseFromServiceResponse(await conversationService.GetConversationByUsers(senderId, pagination, currentUser.Result)) : 
            CreateErrorMessageResult<PagedResponse<ConversationItemDto>>(currentUser.Error);
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<RequestResponse<PagedResponse<UserConversationDto>>>> GetPage([FromQuery]PaginationQueryParams pagination)
    {
        var currentUser = await GetCurrentUser();
        return currentUser.Result != null ? 
            CreateRequestResponseFromServiceResponse(await conversationService.GetConversationsByUsers(currentUser.Result.Id, pagination)) : 
            CreateErrorMessageResult<PagedResponse<UserConversationDto>>(currentUser.Error);
    }
}