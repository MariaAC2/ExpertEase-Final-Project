using Ardalis.Specification;
using ExpertEase.Application.DataTransferObjects.CategoryDTOs;
using ExpertEase.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpertEase.Application.Specifications;

public sealed class CategoryProjectionSpec : Specification<Category, CategoryDto>
{
    private CategoryProjectionSpec(bool orderByName = false)
    {
        Query.Select(e => new CategoryDto
        {
            Id = e.Id,
            Name = e.Name,
            Description = e.Description,
        });
        if (orderByName)
        {
            Query.OrderBy(e => e.Name);
        }
    }
    
    public CategoryProjectionSpec(Guid id) : this() => Query.Where(e => e.Id == id);

    public CategoryProjectionSpec(string? search) : this(true)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            Query.Where(c => c.Name.Contains(search)); // or use .ToLower().Contains(search.ToLower())
        }
    }
}

public sealed class CategoryAdminProjectionSpec : Specification<Category, CategoryAdminDto>
{
    private CategoryAdminProjectionSpec(bool orderByCreatedAt = false)
    {
        Query.Select(e => new CategoryAdminDto
        {
            Id = e.Id,
            Name = e.Name,
            Description = e.Description,
            SpecialistsCount = e.Specialists.Count,
            SpecialistIds = e.Specialists.Select(s => s.UserId).ToList()
        });
        if (orderByCreatedAt)
        {
            Query.OrderByDescending(e => e.CreatedAt);
        }
    }
    
    public CategoryAdminProjectionSpec(Guid id) : this() => Query.Where(e => e.Id == id);
    
    public CategoryAdminProjectionSpec(string? search) : this(true)
    {
        search = !string.IsNullOrWhiteSpace(search) ? search.Trim() : null;

        if (search == null)
            return;

        var searchExpr = $"%{search.Replace(" ", "%")}%";

        Query.Where(e =>
            EF.Functions.ILike(e.Name, searchExpr)
        );
    }
}