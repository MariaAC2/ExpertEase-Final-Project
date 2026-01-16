using ExpertEase.Application.DataTransferObjects;
using ExpertEase.Application.DataTransferObjects.ServiceTaskDTOs;
using ExpertEase.Application.Responses;
using ExpertEase.Application.Services;
using ExpertEase.Domain.Enums;
using ExpertEase.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpertEase.API.Controllers;

[ApiController]
[Route("/api/[controller]/[action]")]
public class ServiceTaskController(IUserService userService, IServiceTaskService specialistService): AuthorizedController(userService)
{
    [Authorize]
    [HttpPost("{paymentId:guid}")]
    public async Task<ActionResult<RequestResponse>> AddTaskToPayment(
        [FromRoute] Guid paymentId)
    {
        var currentUser = await GetCurrentUser();
        
        return currentUser.Result != null
            ? CreateRequestResponseFromServiceResponse(await specialistService.CreateServiceTaskFromPayment(paymentId))
            : CreateErrorMessageResult(currentUser.Error);
    }
    
    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RequestResponse<ServiceTaskDto>>> GetById([FromRoute] Guid id)
    {
        var currentUser = await GetCurrentUser();

        return currentUser.Result != null ? 
            CreateRequestResponseFromServiceResponse(await specialistService.GetServiceTask(id)) : 
            CreateErrorMessageResult<ServiceTaskDto>(currentUser.Error);
    }
    
    [Authorize]
    [HttpGet("{otherUserId:guid}")]
    public async Task<ActionResult<RequestResponse<ServiceTaskDto>>> GetCurrent([FromRoute] Guid otherUserId)
    {
        var currentUser = await GetCurrentUser();

        return currentUser.Result != null ? 
            CreateRequestResponseFromServiceResponse(await specialistService.GetCurrentServiceTask(otherUserId, currentUser.Result)) : 
            CreateErrorMessageResult<ServiceTaskDto>(currentUser.Error);
    }
    
    // trebuie sa adaug get page aici
    
    [Authorize(Roles = "Specialist")]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<RequestResponse>> Complete([FromRoute] Guid id)
    {
        var currentUser = await GetCurrentUser();

        var jobStatus = new JobStatusUpdateDto
        {
            Id = id,
            Status = JobStatusEnum.Completed
        };
        return currentUser.Result != null ?
            CreateRequestResponseFromServiceResponse(await specialistService.UpdateServiceTaskStatus(jobStatus, currentUser.Result)) :
            CreateErrorMessageResult(currentUser.Error);
    }
    
    [Authorize(Roles = "Specialist")]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<RequestResponse>> Cancel([FromRoute] Guid id)
    {
        var currentUser = await GetCurrentUser();

        var jobStatus = new JobStatusUpdateDto
        {
            Id = id,
            Status = JobStatusEnum.Cancelled
        };
        return currentUser.Result != null ?
            CreateRequestResponseFromServiceResponse(await specialistService.UpdateServiceTaskStatus(jobStatus, currentUser.Result)) :
            CreateErrorMessageResult(currentUser.Error);
    }
}