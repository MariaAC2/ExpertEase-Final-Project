using System.Globalization;
using Ardalis.Specification;
using ExpertEase.Application.DataTransferObjects.PaymentDTOs;
using ExpertEase.Application.DataTransferObjects.ReplyDTOs;
using ExpertEase.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpertEase.Application.Specifications;

public sealed class ReplyProjectionSpec : Specification<Reply, ReplyDto>
{
    public ReplyProjectionSpec(Guid requestId, bool orderByCreatedAt = false)
    {
        Query.Where(x => x.Id == requestId);
        Query.Select(x => new ReplyDto
        {
            Id = x.Id,
            StartDate = x.StartDate,
            EndDate = x.EndDate,
            Price = x.Price,
            Status = x.Status,
        });
        
        if (orderByCreatedAt)
        {
            Query.OrderByDescending(e => e.CreatedAt);
        }
    }
    
    public ReplyProjectionSpec(Guid id, Guid requestId) : this(requestId) => Query.Where(x => x.RequestId == requestId && x.Id == id);
    
    public ReplyProjectionSpec(string? search, Guid requestId) : this(requestId, true)
    {
        search = !string.IsNullOrWhiteSpace(search) ? search.Trim() : null;

        if (search == null)
            return;

        var searchExpr = $"%{search.Replace(" ", "%")}%";

        Query.Where(r =>
            EF.Functions.ILike(r.Status.ToString(), searchExpr) ||
            EF.Functions.ILike(r.Price.ToString(CultureInfo.InvariantCulture), searchExpr) ||
            EF.Functions.ILike(r.StartDate.ToString(CultureInfo.InvariantCulture), searchExpr) ||
            EF.Functions.ILike(r.EndDate.ToString(CultureInfo.InvariantCulture), searchExpr)
        );
    }
}

public sealed class ReplyPaymentProjectionSpec : Specification<Reply, ReplyPaymentDetailsDto>
{
    public ReplyPaymentProjectionSpec(Guid id)
    {
        Query.Include(e => e.Request);
        Query.Where(x => x.Id == id);
        Query.Select(x => new ReplyPaymentDetailsDto
        {
            ReplyId = x.Id.ToString(),
            StartDate = x.StartDate,
            EndDate = x.EndDate,
            Description = x.Request.Description,
            Address = x.Request.Address,
            Price = x.Price,
            ClientId = x.Request.SenderUserId,
            SpecialistId = x.Request.ReceiverUserId
        });
    }
}