using ExpertEase.Application.DataTransferObjects.FirestoreDTOs;
using ExpertEase.Application.DataTransferObjects.MessageDTOs;
using ExpertEase.Application.DataTransferObjects.UserDTOs;
using ExpertEase.Application.Requests;
using ExpertEase.Application.Responses;
using ExpertEase.Domain.Entities;
using ExpertEase.Infrastructure.Firestore.FirestoreDTOs;

namespace ExpertEase.Application.Services;

public interface IConversationService
{
    Task AddConversation(Conversation conversation, CancellationToken cancellationToken = default);
    Task UpdateConversationRequestId(Guid conversationId, Guid requestId,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<PagedResponse<ConversationItemDto>>> GetConversationByUsers(Guid receiverId,
        PaginationQueryParams pagination,
        UserDto? user = null,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse<PagedResponse<UserConversationDto>>> GetConversationsByUsers(Guid currentUserId,
        PaginationQueryParams pagination,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse> AddConversationItem(
        FirestoreConversationItemAddDto firestoreMessage,
        Guid receiverId,
        UserDto? requestingUser,
        CancellationToken cancellationToken = default);

}