using System.Net;
using ExpertEase.Application.DataTransferObjects.ReviewDTOs;
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
using ExpertEase.Infrastructure.Repositories;

namespace ExpertEase.Infrastructure.Services;

public class ReviewService(IRepository<WebAppDatabaseContext> repository,
    IConversationNotifier conversationNotifier): IReviewService
{
    public async Task<ServiceResponse> AddReview(Guid serviceTaskId, ReviewAddDto review, UserDto? requestingUser = null,
        CancellationToken cancellationToken = default)
    {
        if (requestingUser == null)
        {
            return ServiceResponse.CreateErrorResponse(new (HttpStatusCode.Forbidden, "User not found", ErrorCodes.CannotAdd));
        }
        if (requestingUser.Role == UserRoleEnum.Admin)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden, "Only users can create reviews", ErrorCodes.CannotAdd));
        }
        
        var sender = await repository.GetAsync(new UserSpec(requestingUser.Id), cancellationToken);

        if (sender == null)
        {
            return ServiceResponse.CreateErrorResponse(new (HttpStatusCode.NotFound, "User not found", ErrorCodes.EntityNotFound));
        }

        if (sender.Id != requestingUser.Id)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden, "Only the own user can create reviews", ErrorCodes.CannotAdd));
        }
        
        var receiver = await repository.GetAsync(new UserSpec(review.ReceiverUserId), cancellationToken);
        
        if (receiver == null)
        {
            return ServiceResponse.CreateErrorResponse(new (HttpStatusCode.NotFound, "User with this ID not found", ErrorCodes.EntityNotFound));
        }
        
        var serviceTask = await repository.GetAsync(new ServiceTaskSpec(serviceTaskId), cancellationToken);
        
        if (serviceTask == null)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.NotFound, "Service task not found", ErrorCodes.EntityNotFound));
        }
        
        var reviewEntity = new Review
        {
            SenderUserId = requestingUser.Id,
            SenderUser = sender,
            ReceiverUserId = review.ReceiverUserId,
            ReceiverUser = receiver,
            ServiceTaskId = serviceTaskId,
            ServiceTask = serviceTask,
            Content = review.Content,
            Rating = review.Rating
        };
        
        var existingRequest = await repository.GetAsync(new ReviewSearchSpec(reviewEntity), cancellationToken);
        
        if (existingRequest != null)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Conflict, "Request already exists", ErrorCodes.CannotAdd));
        }
        
        await repository.AddAsync(reviewEntity, cancellationToken);
        
        // Update receiver's average rating
        var allReviews = await repository.ListAsync(new ReviewProjectionSpec(reviewEntity.ReceiverUserId), cancellationToken);
        var average = allReviews.Average(r => r.Rating);
        receiver.Rating = (int)Math.Round(average, MidpointRounding.AwayFromZero);
        await repository.UpdateAsync(receiver, cancellationToken);
        
        // 🆕 Send review notification to the receiver
        await conversationNotifier.NotifyReviewReceived(review.ReceiverUserId, new
        {
            TaskId = serviceTaskId,
            ReviewerName = sender.FullName,
            ReviewerId = requestingUser.Id,
            review.Rating,
            ServiceDescription = serviceTask.Description,
            Message = $"Ai primit o nouă recenzie de {review.Rating} stele de la {sender.FullName}!"
        });

        // 🆕 Check if both parties have now reviewed and update service task status
        await CheckAndUpdateServiceTaskReviewStatus(serviceTaskId, cancellationToken);
        
        return ServiceResponse.CreateSuccessResponse();
    }
        
    public async Task<ServiceResponse<List<Review>>> GetReviewsForServiceTask(Guid serviceTaskId, CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"📋 Getting reviews for service task: {serviceTaskId}");
            
            // Get all reviews for this service task
            var reviews = await repository.ListAsync(new ReviewByServiceTaskSpec(serviceTaskId), cancellationToken);
            
            Console.WriteLine($"📊 Found {reviews.Count} reviews for service task {serviceTaskId}");
            
            return ServiceResponse.CreateSuccessResponse(reviews);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error getting reviews for service task {serviceTaskId}: {ex.Message}");
            return ServiceResponse.CreateErrorResponse<List<Review>>(new(
                HttpStatusCode.InternalServerError, 
                "Error retrieving reviews for service task", 
                ErrorCodes.TechnicalError));
        }
    }

    // 🆕 Check if both parties have reviewed and update service task status
    private async Task CheckAndUpdateServiceTaskReviewStatus(Guid serviceTaskId, CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"🔍 Checking review status for service task: {serviceTaskId}");
            
            var reviews = await repository.ListAsync(new ReviewByServiceTaskSpec(serviceTaskId), cancellationToken);
            
            // If both parties have left reviews (client and specialist)
            if (reviews.Count >= 2)
            {
                Console.WriteLine($"⭐ Both parties have reviewed service task {serviceTaskId} - updating status to Reviewed");
                
                var serviceTask = await repository.GetAsync(new ServiceTaskSpec(serviceTaskId), cancellationToken);
                if (serviceTask != null && serviceTask.Status == JobStatusEnum.Completed)
                {
                    serviceTask.Status = JobStatusEnum.Reviewed;
                    serviceTask.ReviewedAt = DateTime.UtcNow;
                    await repository.UpdateAsync(serviceTask, cancellationToken);
                    
                    // Notify both parties that review process is complete
                    await NotifyReviewProcessComplete(serviceTask);
                }
            }
            else
            {
                Console.WriteLine($"📝 Only {reviews.Count} review(s) submitted for service task {serviceTaskId} - waiting for more");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error checking review status for service task {serviceTaskId}: {ex.Message}");
        }
    }

    // 🆕 Notify both parties that review process is complete
    private async Task NotifyReviewProcessComplete(ServiceTask serviceTask)
    {
        var reviewCompletedPayload = new
        {
            TaskId = serviceTask.Id,
            Message = "Procesul de recenzie a fost finalizat. Mulțumim pentru feedback!",
            ServiceDescription = serviceTask.Description,
            serviceTask.ReviewedAt
        };
        
        await conversationNotifier.NotifyServiceStatusChanged(serviceTask.UserId, reviewCompletedPayload);
        await conversationNotifier.NotifyServiceStatusChanged(serviceTask.SpecialistId, reviewCompletedPayload);
    }

    public async Task<ServiceResponse<ReviewDto>> GetReview(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await repository.GetAsync(new ReviewProjectionSpec(id, userId), cancellationToken);
        
        if (result == null)
        {
            return ServiceResponse.CreateErrorResponse<ReviewDto>(new (HttpStatusCode.NotFound, "Request not found", ErrorCodes.EntityNotFound));
        }

        return ServiceResponse.CreateSuccessResponse(result);
    }
    
    public async Task<ServiceResponse<ReviewAdminDto>> GetReviewAdmin(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await repository.GetAsync(new ReviewAdminProjectionSpec(id), cancellationToken);
        
        if (result == null)
        {
            return ServiceResponse.CreateErrorResponse<ReviewAdminDto>(new (HttpStatusCode.NotFound, "Request not found", ErrorCodes.EntityNotFound));
        }

        return ServiceResponse.CreateSuccessResponse(result);
    }
    
    public async Task<ServiceResponse<PagedResponse<ReviewDto>>> GetReviews(Guid userId, PaginationReviewFilterQueryParams pagination, CancellationToken cancellationToken = default)
    {
        var result = await repository.PageAsync(pagination, new ReviewProjectionSpec(userId, true, pagination.Rating),  cancellationToken);
        
        return ServiceResponse.CreateSuccessResponse(result);
    }
    
    public async Task<ServiceResponse<PagedResponse<ReviewDto>>> GetReviewsList(Guid userId, PaginationQueryParams pagination, CancellationToken cancellationToken = default)
    {
        var result = await repository.PageAsync(pagination, new ReviewProjectionSpec(userId), cancellationToken);
        
        return ServiceResponse.CreateSuccessResponse(result);
    }
    
    public async Task<ServiceResponse<PagedResponse<ReviewAdminDto>>> GetReviewsAdmin(PaginationSearchQueryParams pagination, CancellationToken cancellationToken = default)
    {
        var result = await repository.PageAsync(pagination, new ReviewAdminProjectionSpec(pagination.Search),  cancellationToken);
        
        return ServiceResponse.CreateSuccessResponse(result);
    }

    public async Task<ServiceResponse> UpdateRequest(ReviewUpdateDto review, UserDto? requestingUser = null,
        CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetAsync(new ReviewSpec(review.Id), cancellationToken);

        if (entity == null)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.NotFound, "Request not found", ErrorCodes.EntityNotFound));
        }
        
        entity.Content = review.Content ?? entity.Content;
        entity.Rating = review.Rating ?? entity.Rating;
        
        await repository.UpdateAsync(entity, cancellationToken);
        return ServiceResponse.CreateSuccessResponse();
    }
    
    public async Task<ServiceResponse> DeleteReview(Guid id, UserDto? requestingUser = null,
        CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetAsync(new ReviewSpec(id), cancellationToken);

        if (entity == null)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.NotFound, "Request not found", ErrorCodes.EntityNotFound));
        }
        
        await repository.DeleteAsync<Review>(id, cancellationToken);
        return ServiceResponse.CreateSuccessResponse();
    }

}