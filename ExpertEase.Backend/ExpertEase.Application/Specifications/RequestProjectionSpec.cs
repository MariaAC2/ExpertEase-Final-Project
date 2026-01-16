using Ardalis.Specification;
using ExpertEase.Application.DataTransferObjects.RequestDTOs;
using ExpertEase.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpertEase.Application.Specifications;

public sealed class RequestProjectionSpec : Specification<Request, RequestDto>
{
    private RequestProjectionSpec(bool orderByCreatedAt = false)
    {
        Query.Select(e => new RequestDto
        {
            Id = e.Id,
            RequestedStartDate = e.RequestedStartDate,
            Description = e.Description,
            Status = e.Status,
            SenderPhoneNumber = e.PhoneNumber,
            SenderAddress = e.Address
        });

        if (orderByCreatedAt)
        {
            Query.OrderByDescending(e => e.CreatedAt);
        }
    }
    
    public RequestProjectionSpec(Guid id) : this() => Query.Where(e => e.Id == id);
    // public RequestProjectionSpec(Guid id, Guid userId) : this() => Query.Where(e=>e.SenderUserId == id);
    
    public RequestProjectionSpec(string? search) : this(true) // This constructor will call the first declared constructor with 'true' as the parameter. 
    {
        Query.Include(r => r.SenderUser);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchExpr = $"%{search.Trim().Replace(" ", "%")}%";

            Query.Where(r =>
                    EF.Functions.ILike(r.Description, searchExpr) ||
                    EF.Functions.ILike(r.Status.ToString(), searchExpr) ||
                    EF.Functions.ILike(r.SenderUser.FullName, searchExpr) ||
                    EF.Functions.ILike(r.SenderUser.Email, searchExpr) ||
                    EF.Functions.ILike(r.PhoneNumber, searchExpr) ||
                    EF.Functions.ILike(r.Address, searchExpr)
            );
        }
    }
}

public sealed class RequestConversationProjectionSpec : Specification<Request, RequestDto>
{
    public RequestConversationProjectionSpec(Guid conversationId, bool orderByCreatedAt = false)
    {
        Query.Where(e => e.ConversationId == conversationId.ToString());
        Query.Select(e => new RequestDto
        {
            Id = e.Id,
            RequestedStartDate = e.RequestedStartDate,
            Description = e.Description,
            Status = e.Status,
            SenderPhoneNumber = e.PhoneNumber,
            SenderAddress = e.Address
        });

        if (orderByCreatedAt)
        {
            Query.OrderByDescending(e => e.CreatedAt);
        }
    }
}

public sealed class RequestUserProjectionSpec : Specification<Request, RequestDto>
{
    public RequestUserProjectionSpec(Guid senderUserId, bool orderByCreatedAt = false)
    {
        Query.Where(e => e.SenderUserId == senderUserId);
        Query.Select(e => new RequestDto
        {
            Id = e.Id,
            RequestedStartDate = e.RequestedStartDate,
            Description = e.Description,
            Status = e.Status,
            SenderPhoneNumber = e.PhoneNumber,
            SenderAddress = e.Address
        });

        if (orderByCreatedAt)
        {
            Query.OrderByDescending(e => e.CreatedAt);
        }
    }
    
    public RequestUserProjectionSpec(Guid senderUserId, Guid receiverUserId) : this(senderUserId)
    {
        Query.Where(e => e.ReceiverUserId == receiverUserId);
    }
    public RequestUserProjectionSpec(Guid id, Guid senderUserId, Guid receiverUserId) : this(senderUserId, receiverUserId) => Query.Where(e => e.Id == id);

    public RequestUserProjectionSpec(string? search, Guid senderUserId, Guid receiverUserId) : this(senderUserId, receiverUserId)
    {
        if (string.IsNullOrWhiteSpace(search)) return;
        var searchExpr = $"%{search.Trim().Replace(" ", "%")}%";

        Query.Where(r =>
            r.SenderUserId == senderUserId &&
            EF.Functions.ILike(r.SenderUser.FullName, searchExpr) ||
            EF.Functions.ILike(r.SenderUser.Email, searchExpr) ||
            EF.Functions.ILike(r.PhoneNumber, searchExpr) ||
            EF.Functions.ILike(r.Address, searchExpr)
        );
    }
}