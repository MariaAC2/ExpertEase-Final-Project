using Ardalis.Specification;
using ExpertEase.Application.DataTransferObjects;
using ExpertEase.Application.DataTransferObjects.PaymentDTOs;
using ExpertEase.Application.DataTransferObjects.ReplyDTOs;
using ExpertEase.Application.DataTransferObjects.UserDTOs;
using ExpertEase.Application.Requests;
using ExpertEase.Application.Responses;
using ExpertEase.Domain.Entities;

namespace ExpertEase.Application.Services;

public interface IReplyService
{
    Task<ServiceResponse<ReplyPaymentDetailsDto>> GetReply(Guid replyId, CancellationToken cancellationToken = default);
    Task<ServiceResponse<PagedResponse<ReplyDto>>> GetReplies(Specification<Reply, ReplyDto> spec, PaginationSearchQueryParams pagination, CancellationToken cancellationToken = default);
    // public Task<ServiceResponse<int>> GetUserCount(CancellationToken cancellationToken = default);
    Task<ServiceResponse> AddReply(Guid requestId, ReplyAddDto reply, UserDto? requestingUser = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse> UpdateReply(ReplyUpdateDto reply, UserDto? requestingUser = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse> UpdateReplyStatus(StatusUpdateDto reply, UserDto? requestingUser = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse> DeleteReply(Guid id, UserDto? requestingUser = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse> ConfirmReplyPayment(Guid replyId, UserDto? requestingUser = null,
        CancellationToken cancellationToken = default);
}