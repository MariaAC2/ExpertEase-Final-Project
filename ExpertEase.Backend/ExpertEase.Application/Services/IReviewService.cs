using ExpertEase.Application.DataTransferObjects.RequestDTOs;
using ExpertEase.Application.DataTransferObjects.ReviewDTOs;
using ExpertEase.Application.DataTransferObjects.UserDTOs;
using ExpertEase.Application.Requests;
using ExpertEase.Application.Responses;
using ExpertEase.Domain.Entities;

namespace ExpertEase.Application.Services;

public interface IReviewService
{
    Task<ServiceResponse<ReviewDto>> GetReview(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<ServiceResponse<ReviewAdminDto>> GetReviewAdmin(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResponse<PagedResponse<ReviewDto>>> GetReviews(Guid userId, PaginationReviewFilterQueryParams pagination, CancellationToken cancellationToken = default);
    Task<ServiceResponse<PagedResponse<ReviewDto>>> GetReviewsList(Guid userId, PaginationQueryParams pagination,
        CancellationToken cancellationToken = default);
    Task<ServiceResponse<PagedResponse<ReviewAdminDto>>> GetReviewsAdmin(PaginationSearchQueryParams pagination, CancellationToken cancellationToken = default);

    Task<ServiceResponse<List<Review>>> GetReviewsForServiceTask(Guid serviceTaskId,
        CancellationToken cancellationToken = default);
    Task<ServiceResponse> AddReview(Guid serviceTaskId, ReviewAddDto review, UserDto? requestingUser = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse> UpdateRequest(ReviewUpdateDto review, UserDto? requestingUser = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse> DeleteReview(Guid id, UserDto? requestingUser = null, CancellationToken cancellationToken = default);
}