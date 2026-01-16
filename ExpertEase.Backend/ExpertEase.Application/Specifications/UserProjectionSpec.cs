using Ardalis.Specification;
using ExpertEase.Application.DataTransferObjects.UserDTOs;
using ExpertEase.Domain.Entities;
using ExpertEase.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpertEase.Application.Specifications;

/// <summary>
/// This is a specification to filter the user entities and map it to and UserDTO object via the constructors.
/// The specification will project the entity onto a DTO so it isn't tracked by the framework.
/// Note how the constructors call other constructors which can be used to chain them. Also, this is a sealed class, meaning it cannot be further derived.
/// </summary>
public sealed class UserProjectionSpec : Specification<User, UserDto>
{
    /// <summary>
    /// In this constructor is the projection/mapping expression used to get UserDTO object directly from the database.
    /// </summary>
    private UserProjectionSpec(bool orderByCreatedAt = false)
    {
        Query.Include(e => e.SpecialistProfile)
            .ThenInclude(e => e!.Categories);
        
        Query.Select(e => new UserDto
        {
            Id = e.Id,
            Email = e.Email,
            FullName = e.FullName,
            Role = e.Role,
            AuthProvider = e.AuthProvider,
            StripeCustomerId = e.StripeCustomerId,
        });

        if (orderByCreatedAt)
        {
            Query.OrderByDescending(e => e.CreatedAt);
        }
    }

    public UserProjectionSpec(Guid id) : this()
    {
        Query.Where(e => e.Id == id);
    }
}

public sealed class UserPaymentDetailsProjectionSpec : Specification<User, UserPaymentDetailsDto>
{
    public UserPaymentDetailsProjectionSpec(Guid userId)
    {
        Query
            .Where(u => u.Id == userId)
            .Include(u => u.ContactInfo);
        Query.Select(u => new UserPaymentDetailsDto 
        {
            UserId = u.Id,
            UserFullName = u.FullName,
            Email = u.Email,
            PhoneNumber = u.ContactInfo!.PhoneNumber,
        });
    }
}

public sealed class AdminUserProjectionSpec: Specification<User, UserDto>
{
    private AdminUserProjectionSpec(Guid adminId, bool orderByCreatedAt = false)
    {
        Query.Include(e => e.SpecialistProfile)
            .ThenInclude(e => e!.Categories);
        
        Query.Where(e => e.Id != adminId && e.Role != UserRoleEnum.Specialist);
        Query.Select(e => new UserDto
        {
            Id = e.Id,
            Email = e.Email,
            FullName = e.FullName,
            Role = e.Role,
        });

        if (orderByCreatedAt)
        {
            Query.OrderByDescending(e => e.CreatedAt);
        }
    }
    public AdminUserProjectionSpec(Guid id, Guid adminId) : this(adminId)
    {
        Query.Where(e => e.Id == id);
    }
    
    public AdminUserProjectionSpec(string? search, Guid adminId) : this(adminId, true)
    {
        if (string.IsNullOrWhiteSpace(search)) return;
        var searchExpr = $"%{search.Trim().Replace(" ", "%")}%";
        Query.Where(e =>
            EF.Functions.ILike(e.FullName, searchExpr));
    }
}

public sealed class UserDetailsProjectionSpec : Specification<User, UserDetailsDto>
{
    public UserDetailsProjectionSpec(Guid userId)
    {
        Query
            .Where(u => u.Id == userId)
            .Include(u => u.ContactInfo)
            .Include(u => u.SpecialistProfile!)
                .ThenInclude(sp => sp.Categories);
        Query.Select(u => new UserDetailsDto
            {
                FullName = u.FullName,
                ProfilePictureUrl = u.ProfilePictureUrl,
                Rating = u.Rating,

                // Include these only for specialists
                Email = u.Role == UserRoleEnum.Specialist ? u.Email : null,
                PhoneNumber = u.Role == UserRoleEnum.Specialist ? u.ContactInfo != null ? u.ContactInfo.PhoneNumber : null : null,
                Address = u.Role == UserRoleEnum.Specialist ? u.ContactInfo != null ? u.ContactInfo.Address : null : null,
                YearsExperience = u.Role == UserRoleEnum.Specialist && u.SpecialistProfile != null ? u.SpecialistProfile.YearsExperience : null,
                Description = u.Role == UserRoleEnum.Specialist && u.SpecialistProfile != null ? u.SpecialistProfile.Description : null,
                Portfolio = u.Role == UserRoleEnum.Specialist && u.SpecialistProfile != null ? u.SpecialistProfile.Portfolio.ToList() : null,
                Categories = u.Role == UserRoleEnum.Specialist && u.SpecialistProfile != null ? u.SpecialistProfile.Categories.Select(c => c.Name).ToList() : null
            });
    }
}

public sealed class UserProfileProjectionSpec : Specification<User, UserProfileDto>
{
    public UserProfileProjectionSpec(Guid userId)
    {
        Query
            .Where(u => u.Id == userId)
            .Include(u => u.ContactInfo)
            .Include(u => u.SpecialistProfile!)
            .ThenInclude(sp => sp.Categories);
        Query.Select(u => new UserProfileDto
        {
            Id = u.Id,
            FullName = u.FullName,
            ProfilePictureUrl = u.ProfilePictureUrl,
            Rating = u.Rating,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt,

            // Include these only for specialists
            Email = u.Email,
            PhoneNumber = u.ContactInfo != null ? u.ContactInfo.PhoneNumber : null,
            Address = u.ContactInfo != null ? u.ContactInfo.Address : null,
            YearsExperience = u.Role == UserRoleEnum.Specialist && u.SpecialistProfile != null ? u.SpecialistProfile.YearsExperience : null,
            Description = u.Role == UserRoleEnum.Specialist && u.SpecialistProfile != null ? u.SpecialistProfile.Description : null,
            StripeAccountId = u.Role == UserRoleEnum.Specialist && u.SpecialistProfile != null ? u.SpecialistProfile.StripeAccountId : null,
            Portfolio = u.Role == UserRoleEnum.Specialist && u.SpecialistProfile != null ? u.SpecialistProfile.Portfolio.ToList() : null,
            Categories = u.Role == UserRoleEnum.Specialist && u.SpecialistProfile != null ? u.SpecialistProfile.Categories.Select(c => c.Name).ToList() : null
        });
    }
}