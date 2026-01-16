using System.Net;
using ExpertEase.Application.DataTransferObjects.SpecialistDTOs;
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

public class SpecialistService(IRepository<WebAppDatabaseContext> repository,
    IStripeAccountService stripeAccountService) : ISpecialistService
{
    public async Task<ServiceResponse> AddSpecialist(SpecialistAddDto user, UserDto? requestingUser,
        CancellationToken cancellationToken = default)
    {
        if (requestingUser != null &&
            requestingUser.Role !=
            UserRoleEnum.Admin) // Verify who can add the user, you can change this however you se fit.
            return ServiceResponse.CreateErrorResponse(new ErrorMessage(HttpStatusCode.Forbidden,
                "Only the admin can add users!", ErrorCodes.CannotAdd));

        var result = await repository.GetAsync(new UserSpec(user.Email), cancellationToken);

        if (result != null)
            return ServiceResponse.CreateErrorResponse(new ErrorMessage(HttpStatusCode.Conflict,
                "The user already exists!", ErrorCodes.UserAlreadyExists));
        
        var stripeAccountId = await stripeAccountService.CreateConnectedAccount(user.Email);

        var newUser = new User
        {
            Email = user.Email,
            FullName = user.FullName,
            Role = UserRoleEnum.Specialist,
            Password = user.Password,
            ContactInfo = new ContactInfo
            {
                PhoneNumber = user.PhoneNumber,
                Address = user.Address
            },
            SpecialistProfile = new SpecialistProfile
            {
                YearsExperience = user.YearsExperience,
                Description = user.Description,
                StripeAccountId = stripeAccountId,
                Categories = new List<Category>()
            }
        };

        await repository.AddAsync(newUser, cancellationToken);

        return ServiceResponse.CreateSuccessResponse();
    }

    public async Task<ServiceResponse<SpecialistDto>> GetSpecialist(Guid id, UserDto? requestingUser = null,
        CancellationToken cancellationToken = default)
    {
        var result = await repository.GetAsync(new SpecialistProjectionSpec(id), cancellationToken);

        return result != null
            ? ServiceResponse.CreateSuccessResponse(result)
            : ServiceResponse.CreateErrorResponse<SpecialistDto>(CommonErrors.UserNotFound);
    }

    public async Task<ServiceResponse<PagedResponse<SpecialistDto>>> GetSpecialists(
        SpecialistPaginationQueryParams pagination, CancellationToken cancellationToken = default)
    {
        var result = await repository.PageAsync(pagination, new SpecialistProjectionSpec(pagination),
            cancellationToken);

        return ServiceResponse.CreateSuccessResponse(result);
    }

    public async Task<ServiceResponse<PagedResponse<SpecialistDto>>> SearchSpecialistsByCategory(Guid categoryId, 
        PaginationQueryParams pagination, CancellationToken cancellationToken = default)
    {
        var searchParams = new SpecialistPaginationQueryParams
        {
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            Filters = new SpecialistFilterParams 
            { 
                CategoryIds = new List<string> { categoryId.ToString() } 
            }
        };

        var result = await repository.PageAsync(searchParams, new SpecialistProjectionSpec(searchParams),
            cancellationToken);

        return ServiceResponse.CreateSuccessResponse(result);
    }

    public async Task<ServiceResponse<PagedResponse<SpecialistDto>>> SearchSpecialistsByRatingRange(int minRating, 
        int maxRating, PaginationQueryParams pagination, CancellationToken cancellationToken = default)
    {
        var searchParams = new SpecialistPaginationQueryParams
        {
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            Filters = new SpecialistFilterParams
            {
                MinRating = minRating
                // Note: MaxRating is removed from the new structure as per frontend interface
            }
        };
        
        var result = await repository.PageAsync(searchParams, new SpecialistProjectionSpec(searchParams),
            cancellationToken);

        return ServiceResponse.CreateSuccessResponse(result);
    }

    public async Task<ServiceResponse<PagedResponse<SpecialistDto>>> SearchSpecialistsByExperienceRange(string experienceRange, 
        PaginationQueryParams pagination, CancellationToken cancellationToken = default)
    {
        var searchParams = new SpecialistPaginationQueryParams
        {
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            Filters = new SpecialistFilterParams
            {
                ExperienceRange = experienceRange
            }
        };
        
        var result = await repository.PageAsync(searchParams, new SpecialistProjectionSpec(searchParams),
            cancellationToken);

        return ServiceResponse.CreateSuccessResponse(result);
    }

    public async Task<ServiceResponse<PagedResponse<SpecialistDto>>> GetTopRatedSpecialists(
        PaginationQueryParams pagination, CancellationToken cancellationToken = default)
    {
        var searchParams = new SpecialistPaginationQueryParams
        {
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            Filters = new SpecialistFilterParams
            {
                SortByRating = "desc" // Sort by rating in descending order
            }
        };
        
        var result = await repository.PageAsync(searchParams, new SpecialistProjectionSpec(searchParams),
            cancellationToken);

        return ServiceResponse.CreateSuccessResponse(result);
    }

    public async Task<ServiceResponse> UpdateSpecialist(SpecialistUpdateDto user, UserDto? requestingUser = null,
        CancellationToken cancellationToken = default)
    {
        if (requestingUser != null && requestingUser.Role != UserRoleEnum.Admin)
            return ServiceResponse.CreateErrorResponse(new ErrorMessage(HttpStatusCode.Forbidden,
                "Only the admin can update users!", ErrorCodes.CannotAdd));

        var result = await repository.GetAsync(new UserSpec(user.Id), cancellationToken);

        if (result == null)
            return ServiceResponse.CreateErrorResponse(new ErrorMessage(HttpStatusCode.NotFound,
                "The user doesn't exist!", ErrorCodes.EntityNotFound));

        // Safely update fields with null-checks
        result.FullName = user.FullName ?? result.FullName;

        if (result.ContactInfo != null)
        {
            result.ContactInfo.PhoneNumber = user.PhoneNumber ?? result.ContactInfo.PhoneNumber;
            result.ContactInfo.Address = user.Address ?? result.ContactInfo.Address;
        }

        if (result.SpecialistProfile != null)
        {
            result.SpecialistProfile.YearsExperience = user.YearsExperience ?? result.SpecialistProfile.YearsExperience;
            result.SpecialistProfile.Description = user.Description ?? result.SpecialistProfile.Description;
        }

        await repository.UpdateAsync(result, cancellationToken);

        return ServiceResponse.CreateSuccessResponse();
    }

    public async Task<ServiceResponse> DeleteSpecialist(Guid id, UserDto? requestingUser = null,
        CancellationToken cancellationToken = default)
    {
        if (requestingUser != null && requestingUser.Role != UserRoleEnum.Admin &&
            requestingUser.Id != id) // Verify who can add the user, you can change this however you se fit.
            return ServiceResponse.CreateErrorResponse(new ErrorMessage(HttpStatusCode.Forbidden,
                "Only the admin or the own user can delete the user!", ErrorCodes.CannotDelete));

        await repository.DeleteAsync<User>(id, cancellationToken); // Delete the entity.

        return ServiceResponse.CreateSuccessResponse();
    }
}