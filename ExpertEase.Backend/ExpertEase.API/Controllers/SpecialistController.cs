using System.Net;
using ExpertEase.Application.DataTransferObjects.SpecialistDTOs;
using ExpertEase.Application.DataTransferObjects.UserDTOs;
using ExpertEase.Application.Errors;
using ExpertEase.Application.Requests;
using ExpertEase.Application.Responses;
using ExpertEase.Application.Services;
using ExpertEase.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpertEase.API.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class SpecialistController(IUserService userService, ISpecialistService specialistService) : AuthorizedController(userService)
{
    [Authorize(Roles = "Admin")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RequestResponse<SpecialistDto>>> GetById([FromRoute] Guid id)
    {
        var result = await GetCurrentUser();
        
        return result.Result != null ?
            CreateRequestResponseFromServiceResponse(await specialistService.GetSpecialist(id, result.Result)) :
            CreateErrorMessageResult<SpecialistDto>(result.Error);
    }
        
    // Updated GetPage method to match frontend structure exactly
    [HttpGet]
    public async Task<ActionResult<RequestResponse<PagedResponse<SpecialistDto>>>> GetPage(
        [FromQuery] PaginationSearchQueryParams pagination,
        [FromQuery] SpecialistFilterParams? filters = null)
    {
        // Validate filter parameters if provided
        if (filters != null)
        {
            // Validate rating if provided
            if (filters.MinRating is < 0 or > 5)
            {
                return CreateErrorMessageResult<PagedResponse<SpecialistDto>>(
                    new ErrorMessage(HttpStatusCode.BadRequest, 
                    "Invalid rating. Rating must be between 0 and 5."));
            }

            // Validate experience range if provided
            if (!string.IsNullOrWhiteSpace(filters.ExperienceRange))
            {
                var validRanges = new[] { "0-2", "2-5", "5-7", "7-10", "10+" };
                if (!validRanges.Contains(filters.ExperienceRange.ToLowerInvariant()))
                {
                    return CreateErrorMessageResult<PagedResponse<SpecialistDto>>(
                        new ErrorMessage(HttpStatusCode.BadRequest,
                        "Invalid experience range. Valid ranges are: 0-2, 2-5, 5-7, 7-10, 10+"));
                }
            }

            // Validate sort parameter if provided
            if (!string.IsNullOrWhiteSpace(filters.SortByRating))
            {
                var validSorts = new[] { "asc", "desc" };
                if (!validSorts.Contains(filters.SortByRating.ToLowerInvariant()))
                {
                    return CreateErrorMessageResult<PagedResponse<SpecialistDto>>(
                        new ErrorMessage(HttpStatusCode.BadRequest,
                        "Invalid sort parameter. Valid values are: asc, desc"));
                }
            }
        }

        var specialistPagination = new SpecialistPaginationQueryParams
        {
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            Search = pagination.Search,
            Filters = filters
        };

        return CreateRequestResponseFromServiceResponse(await specialistService.GetSpecialists(specialistPagination));
    }

    // Update the legacy methods to use the new structure:

    [HttpGet]
    public async Task<ActionResult<RequestResponse<PagedResponse<SpecialistDto>>>> SearchByCategory([FromQuery] Guid categoryId, [FromQuery] PaginationQueryParams pagination)
    {
        return await GetPage(
            new PaginationSearchQueryParams 
            { 
                Page = pagination.Page, 
                PageSize = pagination.PageSize 
            },
            new SpecialistFilterParams 
            { 
                CategoryIds = new List<string> { categoryId.ToString() }
            });
    }

    [HttpGet]
    public async Task<ActionResult<RequestResponse<PagedResponse<SpecialistDto>>>> SearchByRatingRange([FromQuery] int minRating, [FromQuery] int maxRating, [FromQuery] PaginationQueryParams pagination)
    {
        if (minRating < 0 || minRating > 5 || maxRating < 0 || maxRating > 5 || minRating > maxRating)
        {
            return CreateErrorMessageResult<PagedResponse<SpecialistDto>>(new ErrorMessage(HttpStatusCode.BadRequest,
                "Invalid rating range."));
        }

        return await GetPage(
            new PaginationSearchQueryParams 
            { 
                Page = pagination.Page, 
                PageSize = pagination.PageSize 
            },
            new SpecialistFilterParams 
            { 
                MinRating = minRating
                // Note: MaxRating is removed from the new structure as per frontend interface
            });
    }

    [HttpGet]
    public async Task<ActionResult<RequestResponse<PagedResponse<SpecialistDto>>>> SearchByExperienceRange([FromQuery] string experienceRange, [FromQuery] PaginationQueryParams pagination)
    {
        var validRanges = new[] { "0-2", "2-5", "5-7", "7-10", "10+" };
        
        if (string.IsNullOrWhiteSpace(experienceRange) || !validRanges.Contains(experienceRange.ToLowerInvariant()))
        {
            return CreateErrorMessageResult<PagedResponse<SpecialistDto>>(new ErrorMessage(HttpStatusCode.BadRequest,
                "Invalid experience range. Valid ranges are: 0-2, 2-5, 5-7, 7-10, 10+"));
        }

        return await GetPage(
            new PaginationSearchQueryParams 
            { 
                Page = pagination.Page, 
                PageSize = pagination.PageSize 
            },
            new SpecialistFilterParams 
            { 
                ExperienceRange = experienceRange
            });
    }

    [HttpGet]
    public async Task<ActionResult<RequestResponse<PagedResponse<SpecialistDto>>>> GetTopRated([FromQuery] PaginationQueryParams pagination)
    {
        return await GetPage(
            new PaginationSearchQueryParams 
            { 
                Page = pagination.Page, 
                PageSize = pagination.PageSize 
            },
            new SpecialistFilterParams 
            { 
                SortByRating = "desc"
            });
    }
    
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<RequestResponse>> Add([FromBody] SpecialistAddDto user)
    {
        var currentUser = await GetCurrentUser();
        var newUser = user with
        {
            Password = PasswordUtils.HashPassword(user.Password)
        };
    
        return currentUser.Result != null ?
            CreateRequestResponseFromServiceResponse(await specialistService.AddSpecialist(newUser, currentUser.Result)) :
            CreateErrorMessageResult(currentUser.Error);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<RequestResponse>> Update([FromBody] SpecialistUpdateDto specialist)
    {
        var currentUser = await GetCurrentUser();

        if (currentUser.Result == null)
        {
            return CreateErrorMessageResult(currentUser.Error);
        }

        var response = await specialistService.UpdateSpecialist(specialist, currentUser.Result);

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
}