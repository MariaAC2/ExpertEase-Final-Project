using System.Net;
using Ardalis.Specification;
using ExpertEase.Application.DataTransferObjects;
using ExpertEase.Application.DataTransferObjects.FirestoreDTOs;
using ExpertEase.Application.DataTransferObjects.RequestDTOs;
using ExpertEase.Application.DataTransferObjects.UserDTOs;
using ExpertEase.Application.Errors;
using ExpertEase.Application.Requests;
using ExpertEase.Application.Responses;
using ExpertEase.Application.Services;
using ExpertEase.Application.Specifications;
using ExpertEase.Domain.Entities;
using ExpertEase.Domain.Enums;
using ExpertEase.Domain.Specifications;
using ExpertEase.Infrastructure.Database;
using ExpertEase.Infrastructure.Firestore.FirestoreDTOs;
using ExpertEase.Infrastructure.Firestore.FirestoreRepository;
using ExpertEase.Infrastructure.Repositories;
using Google.Cloud.Firestore;

namespace ExpertEase.Infrastructure.Services;

public class RequestService(IRepository<WebAppDatabaseContext> repository, 
    IFirestoreRepository firestoreRepository, 
    IConversationService conversationService,
    IConversationNotifier notificationService) : IRequestService
{
    public async Task<ServiceResponse> AddRequest(RequestAddDto request, UserDto? requestingUser = null, CancellationToken cancellationToken = default)
    {
        if (requestingUser == null)
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden, "User not found", ErrorCodes.CannotAdd));

        if (requestingUser.Role != UserRoleEnum.Client)
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden, "Only users can create requests", ErrorCodes.CannotAdd));

        var sender = await repository.GetAsync(new UserSpec(requestingUser.Id), cancellationToken);
        var receiver = await repository.GetAsync(new UserSpec(request.ReceiverUserId), cancellationToken);

        if (sender == null || receiver is not { Role: UserRoleEnum.Specialist })
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden, "Invalid sender or receiver", ErrorCodes.CannotAdd));

        var conversationKey = $"{requestingUser.Id}_{request.ReceiverUserId}";
        var conversation = await firestoreRepository.GetAsync<FirestoreConversationDto>(
            "conversations",
            q => q.WhereEqualTo("Participants", conversationKey),
            cancellationToken);

        if (conversation == null)
        {
            conversation = new FirestoreConversationDto
            {
                Id = Guid.NewGuid().ToString(),
                ParticipantIds = [requestingUser.Id.ToString(), request.ReceiverUserId.ToString()],
                Participants = conversationKey,
                RequestId = "",
                ClientData = new FirestoreUserConversationDto
                {
                    UserId = requestingUser.Id.ToString(),
                    UserFullName = requestingUser.FullName,
                    UserProfilePictureUrl = requestingUser.ProfilePictureUrl
                },
                SpecialistData = new FirestoreUserConversationDto
                {
                    UserId = receiver.Id.ToString(),
                    UserFullName = receiver.FullName,
                    UserProfilePictureUrl = receiver.ProfilePictureUrl
                },
                CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow)
            };

            await firestoreRepository.AddAsync("conversations", conversation, cancellationToken);
        }
        else
        {
            var existingRequests = await repository.ListAsync(new RequestConversationSpec(conversation.Id), cancellationToken);
            var lastRequest = existingRequests.OrderByDescending(r => r.CreatedAt).FirstOrDefault();

            if (lastRequest != null && lastRequest.Status != StatusEnum.Failed && lastRequest.Status != StatusEnum.Completed)
                return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden, "Cannot create request until last is finalized", ErrorCodes.CannotAdd));
        }

        DateTime normalizedRequestedStartDate = request.RequestedStartDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(request.RequestedStartDate, DateTimeKind.Utc)
            : request.RequestedStartDate.ToUniversalTime();

        var requestEntity = new Request
        {
            SenderUserId = sender.Id,
            SenderUser = sender,
            ReceiverUserId = receiver.Id,
            ReceiverUser = receiver,
            RequestedStartDate = normalizedRequestedStartDate,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            Description = request.Description,
            Status = StatusEnum.Pending,
            ConversationId = conversation.Id
        };

        await repository.AddAsync(requestEntity, cancellationToken);
        
        sender.ContactInfo ??= new ContactInfo
        {
            PhoneNumber = request.PhoneNumber,
            Address = request.Address
        };
        
        await repository.UpdateAsync(sender, cancellationToken);

        var firestoreRequest = new FirestoreConversationItemDto
        {
            Id = requestEntity.Id.ToString(),
            ConversationId = conversation.Id,
            SenderId = sender.Id.ToString(),
            CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow),
            Type = "request",
            Data = new Dictionary<string, object>
            {
                { "RequestedStartDate", Timestamp.FromDateTime(request.RequestedStartDate) },
                { "PhoneNumber", request.PhoneNumber },
                { "Address", request.Address },
                { "Description", request.Description },
                { "Status", "Pending" }
            }
        };

        await firestoreRepository.AddAsync("conversationElements", firestoreRequest, cancellationToken);
        await conversationService.UpdateConversationRequestId(Guid.Parse(conversation.Id), requestEntity.Id, cancellationToken);

        return ServiceResponse.CreateSuccessResponse();
    }


    public async Task<ServiceResponse<RequestDto>> GetRequest(Guid requestId, CancellationToken cancellationToken = default)
    {
        var result = await repository.GetAsync(new RequestProjectionSpec(requestId), cancellationToken);
        
        return result == null ? ServiceResponse.CreateErrorResponse<RequestDto>(new (HttpStatusCode.NotFound, "Request not found", ErrorCodes.EntityNotFound)) : ServiceResponse.CreateSuccessResponse(result);
    }

    public async Task<ServiceResponse<PagedResponse<RequestDto>>> GetRequests(Specification<Request, RequestDto> spec,
        PaginationSearchQueryParams pagination, CancellationToken cancellationToken = default)
    {
        var result = await repository.PageAsync(pagination, spec, cancellationToken); // Use the specification and pagination API to get only some entities from the database.

        return ServiceResponse.CreateSuccessResponse(result);
    }
    
    public async Task<ServiceResponse<int>> GetRequestCount(CancellationToken cancellationToken = default) => 
        ServiceResponse.CreateSuccessResponse(await repository.GetCountAsync<Request>(cancellationToken));
    public async Task<ServiceResponse> UpdateRequestStatus(StatusUpdateDto request, UserDto? requestingUser = null,
        CancellationToken cancellationToken = default)
    {
        if (requestingUser is { Role: UserRoleEnum.Client } && request.Status != StatusEnum.Cancelled)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden, "Users cannot accept or reject request", ErrorCodes.CannotUpdate));
        }
        
        var entity = await repository.GetAsync(new RequestSpec(request.Id), cancellationToken);

        if (entity == null)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.NotFound, "Request not found", ErrorCodes.EntityNotFound));
        }
        
        if (entity.Status != StatusEnum.Pending)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden, "Request is already processed", ErrorCodes.CannotUpdate));
        }
        
        // Store original data for notification payload
        var originalSenderId = entity.SenderUserId;
        var specialistId = requestingUser?.Id ?? entity.ReceiverUserId; // The one performing the action
        
        // Update PostgreSQL
        entity.Status = request.Status;
        if (entity.Status == StatusEnum.Rejected)
        {
            entity.RejectedAt = DateTime.UtcNow;
        }
        
        await repository.UpdateAsync(entity, cancellationToken);

        // 🆕 Update Firestore conversation item
        await UpdateFirestoreRequestStatus(entity.Id, request.Status, cancellationToken);
        
        // 🆕 Send SignalR notifications based on status
        await SendRequestStatusNotification(entity, originalSenderId, specialistId, requestingUser?.Id, cancellationToken);
        
        return ServiceResponse.CreateSuccessResponse();
    }

    private async Task SendRequestStatusNotification(
        Request entity, 
        Guid clientId,
        Guid specialistId,
        Guid? actionPerformerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get user details for better notifications
            var client = await repository.GetAsync(new UserSpec(clientId), cancellationToken);
            if (client == null) throw new ArgumentNullException(nameof(client));
            var specialist = await repository.GetAsync(new UserSpec(specialistId), cancellationToken);
            if (specialist == null) throw new ArgumentNullException(nameof(specialist));

            switch (entity.Status)
            {
                case StatusEnum.Accepted:
                    await HandleRequestAccepted(entity, client, specialist);
                    break;
                    
                case StatusEnum.Rejected:
                    await HandleRequestRejected(entity, client, specialist);
                    break;
                    
                case StatusEnum.Cancelled:
                    await HandleRequestCancelled(entity, client, specialist, actionPerformerId);
                    break;
            }
        }
        catch (Exception ex)
        {
            // Log the error but don't fail the main operation
            Console.WriteLine($"Error sending request status notifications: {ex.Message}");
        }
    }

    private async Task HandleRequestAccepted(Request entity, User client, User specialist)
    {
        // Payload for the CLIENT (original sender) - their request was accepted
        var clientPayload = new
        {
            Type = "request_accepted",
            RequestId = entity.Id,
            Status = entity.Status.ToString(),
            SpecialistName = specialist.FullName,
            SpecialistId = specialist.Id,
            entity.Description,
            entity.RequestedStartDate,
            entity.PhoneNumber,
            entity.Address,
            UpdatedAt = DateTime.UtcNow,
            Message = $"Great news! {specialist.FullName} has accepted your service request."
        };

        // Payload for the SPECIALIST (who accepted) - confirmation
        var specialistPayload = new
        {
            Type = "request_accepted_confirmation",
            RequestId = entity.Id,
            Status = entity.Status.ToString(),
            ClientName = client.FullName,
            ClientId = client.Id,
            entity.Description,
            entity.RequestedStartDate,
            entity.PhoneNumber,
            entity.Address,
            UpdatedAt = DateTime.UtcNow,
            Message = $"You have successfully accepted the service request from {client.FullName}."
        };

        // Send notifications to BOTH users
        await notificationService.NotifyRequestAccepted(client.Id, clientPayload);
        await notificationService.NotifyRequestAccepted(specialist.Id, specialistPayload);
    }

    // 🆕 Handle request rejection - notify both users
    private async Task HandleRequestRejected(Request entity, User client, User specialist)
    {
        // Payload for the CLIENT (original sender) - their request was rejected
        var clientPayload = new
        {
            Type = "request_rejected",
            RequestId = entity.Id,
            Status = entity.Status.ToString(),
            SpecialistName = specialist.FullName,
            SpecialistId = specialist.Id,
            entity.Description,
            entity.RequestedStartDate,
            entity.RejectedAt,
            UpdatedAt = DateTime.UtcNow,
            Message = $"Unfortunately, {specialist.FullName} cannot fulfill your service request at this time."
        };

        // Payload for the SPECIALIST (who rejected) - confirmation
        var specialistPayload = new
        {
            Type = "request_rejected_confirmation", 
            RequestId = entity.Id,
            Status = entity.Status.ToString(),
            ClientName = client.FullName,
            ClientId = client.Id,
            entity.Description,
            entity.RequestedStartDate,
            entity.RejectedAt,
            UpdatedAt = DateTime.UtcNow,
            Message = $"You have declined the service request from {client.FullName}."
        };

        // Send notifications to BOTH users
        await notificationService.NotifyRequestRejected(client.Id, clientPayload);
        await notificationService.NotifyRequestRejected(specialist.Id, specialistPayload);
    }

    // 🆕 Handle request cancellation - notify the other party
    private async Task HandleRequestCancelled(Request entity, User client, User specialist, Guid? actionPerformerId)
    {
        if (actionPerformerId == client.Id)
        {
            // Client cancelled their own request - notify the specialist
            var specialistPayload = new
            {
                Type = "request_cancelled",
                RequestId = entity.Id,
                Status = entity.Status.ToString(),
                ClientName = client.FullName,
                ClientId = client.Id,
                entity.Description,
                entity.RequestedStartDate,
                UpdatedAt = DateTime.UtcNow,
                Message = $"{client.FullName} has cancelled their service request."
            };

            await notificationService.NotifyRequestCancelled(specialist.Id, specialistPayload);
        }
        else if (actionPerformerId == specialist.Id)
        {
            // Specialist cancelled - notify the client
            var clientPayload = new
            {
                Type = "request_cancelled",
                RequestId = entity.Id,
                Status = entity.Status.ToString(),
                SpecialistName = specialist.FullName,
                SpecialistId = specialist.Id,
                entity.Description,
                entity.RequestedStartDate,
                UpdatedAt = DateTime.UtcNow,
                Message = $"{specialist.FullName} has cancelled the service request."
            };

            await notificationService.NotifyRequestCancelled(client.Id, clientPayload);
        }
    }

    public async Task<ServiceResponse> UpdateRequest(RequestUpdateDto request, UserDto? requestingUser = null,
        CancellationToken cancellationToken = default)
    {
        if (requestingUser != null && requestingUser.Role != UserRoleEnum.Client && 
            (request.Description != null || request.PhoneNumber != null || request.Address != null || request.RequestedStartDate != null))
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden, "Non users cannot update request info", ErrorCodes.CannotUpdate));
        }
        
        var entity = await repository.GetAsync(new RequestSpec(request.Id), cancellationToken);

        if (entity == null)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.NotFound, "Request not found", ErrorCodes.EntityNotFound));
        }
        
        // Update PostgreSQL
        entity.RequestedStartDate = request.RequestedStartDate ?? entity.RequestedStartDate;
        entity.PhoneNumber = request.PhoneNumber ?? entity.PhoneNumber;
        entity.Address = request.Address ?? entity.Address;
        entity.Description = request.Description ?? entity.Description;
        
        await repository.UpdateAsync(entity, cancellationToken);

        // 🆕 Update Firestore conversation item data
        await UpdateFirestoreRequestData(entity, cancellationToken);
        
        return ServiceResponse.CreateSuccessResponse();
    }
        
    /// <summary>
    /// Update the status of a request in Firestore conversation elements
    /// </summary>
    private async Task UpdateFirestoreRequestStatus(Guid requestId, StatusEnum newStatus, CancellationToken cancellationToken)
    {
        try
        {
            // Find the Firestore conversation item using the PostgreSQL request ID
            var firestoreItem = await firestoreRepository.GetAsync<FirestoreConversationItemDto>(
                "conversationElements", 
                requestId.ToString(), 
                cancellationToken);

            if (firestoreItem != null)
            {
                // Update the status in the data dictionary
                firestoreItem.Data["Status"] = newStatus.ToString();
                
                // Update the Firestore document
                await firestoreRepository.UpdateAsync("conversationElements", firestoreItem, cancellationToken);
            }
            else
            {
                // Log warning if Firestore item not found
                Console.WriteLine($"Warning: Firestore conversation item not found for request ID {requestId}");
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail the main operation
            Console.WriteLine($"Error updating Firestore request status: {ex.Message}");
        }
    }

    /// <summary>
    /// Update request data fields in Firestore conversation elements
    /// </summary>
    private async Task UpdateFirestoreRequestData(Request entity, CancellationToken cancellationToken)
    {
        try
        {
            var firestoreItem = await firestoreRepository.GetAsync<FirestoreConversationItemDto>(
                "conversationElements", 
                entity.Id.ToString(), 
                cancellationToken);

            if (firestoreItem != null)
            {
                // Update the data fields
                firestoreItem.Data["RequestedStartDate"] = Timestamp.FromDateTime(entity.RequestedStartDate);
                firestoreItem.Data["PhoneNumber"] = entity.PhoneNumber;
                firestoreItem.Data["Address"] = entity.Address;
                firestoreItem.Data["Description"] = entity.Description;
                
                await firestoreRepository.UpdateAsync("conversationElements", firestoreItem, cancellationToken);
            }
            else
            {
                Console.WriteLine($"Warning: Firestore conversation item not found for request ID {entity.Id}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating Firestore request data: {ex.Message}");
        }
    }

    public async Task<ServiceResponse> DeleteRequest(Guid id, UserDto? requestingUser = null,
        CancellationToken cancellationToken = default)
    {
        if (requestingUser != null && requestingUser.Role != UserRoleEnum.Admin && requestingUser.Id != id) // Verify who can add the user, you can change this however you se fit.
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden, "Only the admin or the own user can delete the user!", ErrorCodes.CannotDelete));
        }

        await repository.DeleteAsync<Request>(id, cancellationToken);
        return ServiceResponse.CreateSuccessResponse();
    }
}