using Ardalis.Specification;
using ExpertEase.Application.DataTransferObjects.ReviewDTOs;
using ExpertEase.Application.DataTransferObjects.UserDTOs;
using ExpertEase.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpertEase.Application.Specifications;

public sealed class ReviewProjectionSpec : Specification<Review, ReviewDto>
{
    public ReviewProjectionSpec(Guid userId, bool orderByCreatedAt = false)
    {
        Query.Include(e => e.SenderUser);
        Query.Where(e=> e.ReceiverUserId == userId);
        Query.Select(x => new ReviewDto
        {
            SenderUserFullName = x.SenderUser.FullName,
            SenderUserProfilePictureUrl = x.SenderUser.ProfilePictureUrl,
            Rating = x.Rating,
            Content = x.Content
        });

        if (orderByCreatedAt)
        {
            Query.OrderByDescending(e => e.CreatedAt);
        }
    }
    
    public ReviewProjectionSpec(Guid id, Guid userId) : this(userId) => Query.Where(e => e.Id == id);
    
    public ReviewProjectionSpec(Guid userId, bool orderByCreatedAt, int? ratingFilter = null) : this(userId, true)
    {
        if (ratingFilter.HasValue)
        {
            Query.Where(r => r.Rating == ratingFilter.Value);
        }
    }
}

public sealed class UserDetailsReviewProjectionSpec : Specification<Review, ReviewDto>
{
    public UserDetailsReviewProjectionSpec(Guid userId)
    {
        Query.Include(e => e.SenderUser);
        Query.Where(e=> e.ReceiverUserId == userId);
        Query.OrderByDescending(r => r.CreatedAt) // sort newest first
            .Take(5);
        Query.Select(x => new ReviewDto
        {
            SenderUserFullName = x.SenderUser.FullName,
            SenderUserProfilePictureUrl = x.SenderUser.ProfilePictureUrl,
            Rating = x.Rating,
            Content = x.Content
        });
    }
}

public sealed class ReviewByServiceTaskProjectionSpec : Specification<Review, ReviewDto>
{
    public ReviewByServiceTaskProjectionSpec(Guid serviceTaskId)
    {
        Query.Where(e=> e.ReceiverUserId == serviceTaskId);
        Query.Select(x => new ReviewDto
        {
            SenderUserFullName = x.SenderUser.FullName,
            SenderUserProfilePictureUrl = x.SenderUser.ProfilePictureUrl,
            Rating = x.Rating,
            Content = x.Content
        });
    }
}

public sealed class ReviewAdminProjectionSpec : Specification<Review, ReviewAdminDto>
{
    private ReviewAdminProjectionSpec(bool orderByCreatedAt = false)
    {
        Query.Include(e => e.SenderUser);
        Query.Include(e => e.ReceiverUser);
        Query.Select(x => new ReviewAdminDto
        {
            Id = x.Id,
            SenderUserId = x.SenderUserId,
            ReceiverUserId = x.ReceiverUserId,
            ServiceTaskId = x.ServiceTaskId,
            Content = x.Content,
            Rating = x.Rating
        });

        if (orderByCreatedAt)
        {
            Query.OrderByDescending(e => e.CreatedAt);
        }
    }
    
    public ReviewAdminProjectionSpec(Guid id) : this() => Query.Where(e => e.Id == id);
    
    public ReviewAdminProjectionSpec(string? search) : this(true) 
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchExpr = $"%{search.Trim().Replace(" ", "%")}%";

            Query.Where(r =>
                EF.Functions.ILike(r.SenderUser.Id.ToString(), searchExpr) ||
                EF.Functions.ILike(r.ReceiverUser.Id.ToString(), searchExpr) ||
                EF.Functions.ILike(r.Content, searchExpr) ||
                EF.Functions.ILike(r.Rating.ToString(), searchExpr)
            );
        }
    }
}