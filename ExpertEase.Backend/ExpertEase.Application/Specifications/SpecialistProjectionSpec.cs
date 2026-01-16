using Ardalis.Specification;
using ExpertEase.Application.DataTransferObjects.CategoryDTOs;
using ExpertEase.Application.DataTransferObjects.SpecialistDTOs;
using ExpertEase.Application.Requests;
using ExpertEase.Domain.Entities;
using ExpertEase.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpertEase.Application.Specifications;

public sealed class SpecialistProjectionSpec: Specification<User, SpecialistDto>
{
    private SpecialistProjectionSpec(bool orderByCreatedAt = false)
    {
        Query.Include(e => e.ContactInfo);
        Query.Include(e => e.SpecialistProfile)
            .ThenInclude(e => e.Categories);
        Query.Select(e => new SpecialistDto
        {
            Id = e.Id,
            FullName = e.FullName,
            Email = e.Email,
            ProfilePictureUrl = e.ProfilePictureUrl,
            PhoneNumber = e.ContactInfo != null ? e.ContactInfo.PhoneNumber : "",
            Address = e.ContactInfo != null ? e.ContactInfo.Address : "",
            YearsExperience = e.SpecialistProfile != null ? e.SpecialistProfile.YearsExperience : 0,
            Description = e.SpecialistProfile != null ? e.SpecialistProfile.Description : "",
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt,
            Rating = e.Rating,
            Categories = e.SpecialistProfile != null
                ? e.SpecialistProfile.Categories.Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description
                }).ToList()
                : new List<CategoryDto>()
        });
        
        if (orderByCreatedAt)
        {
            Query.OrderByDescending(e => e.CreatedAt);
        }
    }
    
    public SpecialistProjectionSpec(Guid id) : this()
    {
        Query.Where(e => e.Id == id && e.Role == UserRoleEnum.Specialist);
    }
    
    // Updated constructor to handle the new filter structure
    public SpecialistProjectionSpec(SpecialistPaginationQueryParams searchParams) : this(true)
    {
        Query.Where(e => e.Role == UserRoleEnum.Specialist);
        
        // Apply search filter first if provided
        ApplySearchFilter(searchParams.Search);
        
        // Apply filters if provided
        if (searchParams.Filters != null)
        {
            ApplyCategoryFilters(searchParams.Filters.CategoryIds);
            ApplyRatingFilters(searchParams.Filters.MinRating);
            ApplyExperienceRangeFilter(searchParams.Filters.ExperienceRange);
            ApplyRatingSorting(searchParams.Filters.SortByRating);
        }
    }
    
    private void ApplySearchFilter(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return;

        var searchTerm = search.Trim();
        var searchExpr = $"%{searchTerm.Replace(" ", "%")}%";

        Query.Where(e =>
            EF.Functions.ILike(e.FullName, searchExpr) ||
            EF.Functions.ILike(e.Email, searchExpr) ||
            (e.ContactInfo != null && EF.Functions.ILike(e.ContactInfo.PhoneNumber, searchExpr)) ||
            (e.ContactInfo != null && EF.Functions.ILike(e.ContactInfo.Address, searchExpr)) ||
            (e.SpecialistProfile != null && EF.Functions.ILike(e.SpecialistProfile.Description, searchExpr)) ||
            (e.SpecialistProfile != null && e.SpecialistProfile.Categories.Any(c => EF.Functions.ILike(c.Name, searchExpr)))
        );
    }
    
    private void ApplyCategoryFilters(List<string>? categoryIds)
    {
        if (categoryIds == null || !categoryIds.Any())
            return;

        // Convert string IDs to Guids
        var validCategoryIds = new List<Guid>();
        foreach (var categoryId in categoryIds)
        {
            if (Guid.TryParse(categoryId, out var parsedId))
            {
                validCategoryIds.Add(parsedId);
            }
        }

        if (validCategoryIds.Any())
        {
            Query.Where(e => e.SpecialistProfile != null && 
                           e.SpecialistProfile.Categories.Any(c => validCategoryIds.Contains(c.Id)));
        }
    }
    
    private void ApplyRatingFilters(int? minRating)
    {
        if (minRating.HasValue)
        {
            Query.Where(e => e.Rating >= minRating.Value);
        }
    }
    
    private void ApplyRatingSorting(string? sortByRating)
    {
        if (string.IsNullOrWhiteSpace(sortByRating))
            return;
            
        switch (sortByRating.ToLowerInvariant())
        {
            case "asc":
                Query.OrderBy(e => e.Rating);
                break;
            case "desc":
                Query.OrderByDescending(e => e.Rating);
                break;
        }
    }
    
    private void ApplyExperienceRangeFilter(string? experienceRange)
    {
        if (string.IsNullOrWhiteSpace(experienceRange))
            return;
            
        switch (experienceRange.ToLowerInvariant())
        {
            case "0-2":
                Query.Where(e => e.SpecialistProfile != null && 
                               e.SpecialistProfile.YearsExperience >= 0 && 
                               e.SpecialistProfile.YearsExperience <= 2);
                break;
            case "2-5":
                Query.Where(e => e.SpecialistProfile != null && 
                               e.SpecialistProfile.YearsExperience > 2 && 
                               e.SpecialistProfile.YearsExperience <= 5);
                break;
            case "5-7":
                Query.Where(e => e.SpecialistProfile != null && 
                               e.SpecialistProfile.YearsExperience > 5 && 
                               e.SpecialistProfile.YearsExperience <= 7);
                break;
            case "7-10":
                Query.Where(e => e.SpecialistProfile != null && 
                               e.SpecialistProfile.YearsExperience > 7 && 
                               e.SpecialistProfile.YearsExperience <= 10);
                break;
            case "10+":
                Query.Where(e => e.SpecialistProfile != null && 
                               e.SpecialistProfile.YearsExperience > 10);
                break;
        }
    }
}