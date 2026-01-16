using System.Net;
using ExpertEase.Application.DataTransferObjects.CategoryDTOs;
using ExpertEase.Application.DataTransferObjects.UserDTOs;
using ExpertEase.Application.Errors;
using ExpertEase.Application.Requests;
using ExpertEase.Application.Responses;
using ExpertEase.Application.Services;
using ExpertEase.Application.Specifications;
using ExpertEase.Domain.Entities;
using ExpertEase.Domain.Enums;
using ExpertEase.Domain.Specifications;
using ExpertEase.Infrastructure.Database;
using ExpertEase.Infrastructure.Repositories;

namespace ExpertEase.Infrastructure.Services;

public class CategoryService(IRepository<WebAppDatabaseContext> repository) : ICategoryService
{
    public async Task<ServiceResponse> AddCategory(CategoryAddDto category, UserDto? requestingUser = null, CancellationToken cancellationToken = default)
    {
        if (requestingUser == null)
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden, "User has to be authenticated",
                ErrorCodes.CannotAdd));
        
        if (requestingUser.Role != UserRoleEnum.Admin)
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden, "Only admin can create categories",
                ErrorCodes.CannotAdd));
        
        var existingCategory = await repository.GetAsync(new CategorySpec(category.Name), cancellationToken);

        if (existingCategory != null)
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Conflict, "Category already exists!",
                ErrorCodes.EntityAlreadyExists));
        
        var newCategory = new Category
        {
            Name = category.Name,
            Description = category.Description
        };
        
        await repository.AddAsync(newCategory, cancellationToken);
        return ServiceResponse.CreateSuccessResponse();
    }
    
    public async Task<ServiceResponse> AddCategoryToSpecialist(CategorySpecialistAddDto category, UserDto? requestingUser = null,
        CancellationToken cancellationToken = default)
    {
        if (requestingUser == null)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden, "User has to be authenticated",
                ErrorCodes.CannotAdd));
        }

        if (requestingUser.Role != UserRoleEnum.Specialist)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden, "Only own specialist can add to its own categories",
                ErrorCodes.CannotAdd));
        }
        
        var specialist = await repository.GetAsync(new SpecialistProfileSpec(requestingUser.Id), cancellationToken);
        
        if (specialist == null)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.NotFound, "Specialist not found",
                ErrorCodes.EntityNotFound));
        }
        
        var categoryToAdd = await repository.GetAsync(new CategorySpec(category.Name), cancellationToken);

        if (categoryToAdd != null)
        {
            specialist.Categories.Add(categoryToAdd);
            await repository.UpdateAsync(specialist, cancellationToken);
        }
        
        return ServiceResponse.CreateSuccessResponse();
    }

    public async Task<ServiceResponse<CategoryAdminDto>> GetCategory(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await repository.GetAsync(new CategoryAdminProjectionSpec(id), cancellationToken);
        
        return result != null ? 
            ServiceResponse.CreateSuccessResponse(result) : 
            ServiceResponse.CreateErrorResponse<CategoryAdminDto>(CommonErrors.EntityNotFound);
    }
    
    public async Task<ServiceResponse<PagedResponse<CategoryAdminDto>>> GetCategoriesAdmin(
        PaginationSearchQueryParams pagination,
        CancellationToken cancellationToken = default)
    {
        var result = await repository.PageAsync(pagination, new CategoryAdminProjectionSpec(pagination.Search), cancellationToken);

        return ServiceResponse.CreateSuccessResponse(result);
    }
    
    public async Task<ServiceResponse<List<CategoryDto>>> GetCategories(string? search = null,
        CancellationToken cancellationToken = default)
    {
        var spec = new CategoryProjectionSpec(search);
        var result = await repository.ListAsync(spec, cancellationToken);

        return ServiceResponse.CreateSuccessResponse(result);
    }
    
    public async Task<ServiceResponse<List<CategoryDto>>> GetCategoriesForSpecialist(Guid specialistId, string? search = null,
        CancellationToken cancellationToken = default)
    {
        var specialist = await repository.GetAsync(new SpecialistProfileSpec(specialistId), cancellationToken);

        if (specialist == null)
        {
            return ServiceResponse.CreateErrorResponse<List<CategoryDto>>(
                new(HttpStatusCode.NotFound, "Specialist not found", ErrorCodes.EntityNotFound));
        }

        var categories = specialist.Categories
            .Where(c => string.IsNullOrWhiteSpace(search) || c.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description
            })
            .ToList();

        return ServiceResponse.CreateSuccessResponse(categories);
    }
    
    public async Task<ServiceResponse<CategoryDto>> GetCategoryForSpecialist(Guid categoryId, Guid specialistUserId, CancellationToken cancellationToken = default)
    {
        var specialist = await repository.GetAsync(new SpecialistProfileSpec(specialistUserId), cancellationToken);

        if (specialist == null)
        {
            return ServiceResponse.CreateErrorResponse<CategoryDto>(
                new(HttpStatusCode.NotFound, "Specialist not found", ErrorCodes.EntityNotFound));
        }

        var category = specialist.Categories.FirstOrDefault(c => c.Id == categoryId);

        if (category == null)
        {
            return ServiceResponse.CreateErrorResponse<CategoryDto>(
                new(HttpStatusCode.NotFound, "Category not assigned to specialist", ErrorCodes.EntityNotFound));
        }

        return ServiceResponse.CreateSuccessResponse(new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        });
    }
    
    public async Task<ServiceResponse> UpdateCategory(CategoryUpdateDto category, UserDto? requestingUser = null,
        CancellationToken cancellationToken = default)
    {
        if (requestingUser == null)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden, "User has to be authenticated",
                ErrorCodes.CannotUpdate));
        }

        if (requestingUser.Role != UserRoleEnum.Admin)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden, "Only admin can update categories",
                ErrorCodes.CannotUpdate));
        }
        
        var entity = await repository.GetAsync(new CategorySpec(category.Id), cancellationToken);

        if (entity == null)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.NotFound, "Category not found",
                ErrorCodes.EntityNotFound));
        }
        
        entity.Name = category.Name ?? entity.Name;
        entity.Description = category.Description ?? entity.Description;
        
        await repository.UpdateAsync(entity, cancellationToken);
        
        return ServiceResponse.CreateSuccessResponse();
    }

    public async Task<ServiceResponse> DeleteCategory(Guid id, UserDto? requestingUser = null,
        CancellationToken cancellationToken = default)
    {
        if (requestingUser == null)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden, "User has to be authenticated",
                ErrorCodes.CannotDelete));
        }

        if (requestingUser.Role != UserRoleEnum.Admin)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden, "Only admin can delete categories",
                ErrorCodes.CannotDelete));
        }
        
        await repository.DeleteAsync(new CategorySpec(id), cancellationToken);
        
        return ServiceResponse.CreateSuccessResponse();
    }

    public async Task<ServiceResponse> DeleteCategoryFromSpecialist(Guid categoryId, UserDto? requestingUser = null,
        CancellationToken cancellationToken = default)
    {
        if (requestingUser == null)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden, "User has to be authenticated",
                ErrorCodes.CannotDelete));
        }

        if (requestingUser.Role != UserRoleEnum.Specialist)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden, "Only a specialist can remove their own categories",
                ErrorCodes.CannotDelete));
        }

        var specialist = await repository.GetAsync(new SpecialistProfileSpec(requestingUser.Id), cancellationToken);

        if (specialist == null)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.NotFound, "Specialist not found",
                ErrorCodes.EntityNotFound));
        }

        var categoryToRemove = specialist.Categories
            .FirstOrDefault(c => c.Id == categoryId);

        if (categoryToRemove == null)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.NotFound, "Category not assigned to this specialist",
                ErrorCodes.EntityNotFound));
        }

        specialist.Categories.Remove(categoryToRemove);
        await repository.UpdateAsync(specialist, cancellationToken);

        return ServiceResponse.CreateSuccessResponse();
    }
}