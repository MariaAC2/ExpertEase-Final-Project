using Ardalis.Specification;
using ExpertEase.Application.DataTransferObjects.ServiceTaskDTOs;
using ExpertEase.Domain.Entities;
using ExpertEase.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpertEase.Application.Specifications;

public sealed class ServiceTaskProjectionSpec: Specification<ServiceTask, ServiceTaskDto>
{
    private ServiceTaskProjectionSpec(bool orderByCreatedAt = false)
    {
        Query.Select(e => new ServiceTaskDto
        {
            Id = e.Id,
            PaymentId = e.PaymentId,
            UserId = e.UserId,
            SpecialistId = e.SpecialistId,
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            Description = e.Description,
            Address = e.Address,
            Price = e.Price,
            Status = e.Status,
            CompletedAt = e.CompletedAt,
            CancelledAt = e.CancelledAt,
        });

        if (orderByCreatedAt)
        {
            Query.OrderByDescending(e => e.CreatedAt);
        }
    }
    
    public ServiceTaskProjectionSpec(Guid id) : this()
    {
        Query.Where(e => e.Id == id);
    }
    
    public ServiceTaskProjectionSpec(Guid userId, Guid specialistId) : this()
    {
        Query.Where(e => e.UserId == userId && e.SpecialistId == specialistId && e.Status != JobStatusEnum.Reviewed);
    }

    public ServiceTaskProjectionSpec(string? search) : this(true)
    {
        search = !string.IsNullOrWhiteSpace(search) ? search.Trim() : null;

        if (search == null)
            return;

        var searchExpr = $"%{search.Replace(" ", "%")}%";
        
        Query.Where(e =>
            EF.Functions.ILike(e.Description, searchExpr) ||
            EF.Functions.ILike(e.Address, searchExpr)
        );
    }
}

public sealed class ServiceTaskDetailsProjectionSpec : Specification<ServiceTask, ServiceTaskDetailsDto>
{
    public ServiceTaskDetailsProjectionSpec(Guid id)
    {
        Query.Where(e => e.Id == id)
            .Include(e => e.User)
            .Include(e => e.Specialist);
        Query.Select(e => new ServiceTaskDetailsDto
        {
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            Description = e.Description,
            Address = e.Address,
            Price = e.Price,
            ClientName = e.User.FullName,
            SpecialistName = e.Specialist.FullName
        });
    }
}