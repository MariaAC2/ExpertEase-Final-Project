using Ardalis.Specification;
using ExpertEase.Application.DataTransferObjects.CategoryDTOs;
using ExpertEase.Application.DataTransferObjects.SpecialistProfileDTOs;
using ExpertEase.Domain.Entities;
using ExpertEase.Domain.Enums;

namespace ExpertEase.Application.Specifications;

public sealed class SpecialistProfileProjectionSpec : Specification<User, SpecialistProfileDto>
{
    public SpecialistProfileProjectionSpec(Guid id)
    {
        Query.Where(e => e.SpecialistProfile != null && e.SpecialistProfile.UserId == id && e.Role == UserRoleEnum.Specialist);
        Query.Include(e => e.SpecialistProfile)
            .ThenInclude(e => e!.Categories);
        Query.Select(e => new SpecialistProfileDto
        {
            YearsExperience = e.SpecialistProfile != null ? e.SpecialistProfile.YearsExperience : 0,
            Description = e.SpecialistProfile != null ? e.SpecialistProfile.Description : string.Empty,
            Categories = e.SpecialistProfile != null ? e.SpecialistProfile.Categories.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name
            }).ToList() : new List<CategoryDto>(),
            PortfolioPhotos = e.SpecialistProfile != null ? e.SpecialistProfile.Portfolio : new List<string?>(),
            StripeAccountId = e.SpecialistProfile != null ? e.SpecialistProfile.StripeAccountId : null,
        });
    }
}

public sealed class StripeAccountIdProjectionSpec : Specification<User, string>
{
    public StripeAccountIdProjectionSpec(Guid id)
    {
        Query.Where(e => e.Id == id);
        Query.Select(e => e.SpecialistProfile!.StripeAccountId);
    }
}
