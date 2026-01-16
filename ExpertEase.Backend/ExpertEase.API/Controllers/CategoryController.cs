using System.Net;
using ExpertEase.Application.DataTransferObjects.CategoryDTOs;
using ExpertEase.Application.Errors;
using ExpertEase.Application.Requests;
using ExpertEase.Application.Responses;
using ExpertEase.Application.Services;
using ExpertEase.Domain.Enums;
using ExpertEase.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpertEase.API.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class CategoryController(IUserService userService, ICategoryService categoryService) : AuthorizedController(userService)
{
    [Authorize(Roles = "Admin, Specialist")]
    [HttpPost]
    public async Task<ActionResult<RequestResponse>> Add([FromBody] CategoryAddDto category)
    {
        var currentUser = await GetCurrentUser();

        if (currentUser.Result == null)
            return CreateErrorMessageResult(currentUser.Error);

        var user = currentUser.Result;

        ServiceResponse response = user.Role switch
        {
            UserRoleEnum.Admin => await categoryService.AddCategory(category, user),
            UserRoleEnum.Specialist => await categoryService.AddCategoryToSpecialist(new CategorySpecialistAddDto(category.Name), user),
            _ => ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden,
                "User not allowed to perform this action", ErrorCodes.CannotAdd))
        };

        return CreateRequestResponseFromServiceResponse(response);
    }
    
    [Authorize(Roles = "Admin")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RequestResponse<CategoryAdminDto>>> GetById([FromRoute] Guid id)
    {
        var currentUser = await GetCurrentUser();

        return currentUser.Result != null ?
            CreateRequestResponseFromServiceResponse(await categoryService.GetCategory(id)) :
            CreateErrorMessageResult<CategoryAdminDto>(currentUser.Error);
    }
    
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<RequestResponse<List<CategoryDto>>>> GetAll([FromQuery] string? search = null)
    {
        return CreateRequestResponseFromServiceResponse(await categoryService.GetCategories(search));
    }
    
    [Authorize(Roles = "Specialist")]
    [HttpGet]
    public async Task<ActionResult<RequestResponse<List<CategoryDto>>>> GetAllForSpecialist([FromQuery] string? search = null)
    {
        var currentUser = await GetCurrentUser();
        return currentUser.Result != null
            ? CreateRequestResponseFromServiceResponse(
                await categoryService.GetCategoriesForSpecialist(currentUser.Result.Id, search))
            : CreateErrorMessageResult<List<CategoryDto>>(currentUser.Error);
    }
    
    [Authorize(Roles = "Admin, SuperAdmin")]
    [HttpGet]
    public async Task<ActionResult<RequestResponse<PagedResponse<CategoryAdminDto>>>> GetPageForAdmin(
        [FromQuery] PaginationSearchQueryParams pagination)
    {
        return CreateRequestResponseFromServiceResponse(await categoryService.GetCategoriesAdmin(pagination));
    }
    
    [Authorize(Roles = "Admin, SuperAdmin")]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<RequestResponse>> Update([FromBody] CategoryUpdateDto category)
    {
        var currentUser = await GetCurrentUser();

        return currentUser.Result != null ?
            CreateRequestResponseFromServiceResponse(await categoryService.UpdateCategory(category, currentUser.Result)) :
            CreateErrorMessageResult(currentUser.Error);
    }
    
    [Authorize(Roles = "Admin, Specialist")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<RequestResponse>> Delete([FromRoute] Guid id)
    {
        var currentUser = await GetCurrentUser();

        if (currentUser.Result == null)
            return CreateErrorMessageResult(currentUser.Error);

        var user = currentUser.Result;

        ServiceResponse response = user.Role switch
        {
            UserRoleEnum.Admin => await categoryService.DeleteCategory(id, user),
            UserRoleEnum.Specialist => await categoryService.DeleteCategoryFromSpecialist(id, user),
            _ => ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden,
                "User not allowed to perform this action", ErrorCodes.CannotDelete))
        };

        return CreateRequestResponseFromServiceResponse(response);
    }
}