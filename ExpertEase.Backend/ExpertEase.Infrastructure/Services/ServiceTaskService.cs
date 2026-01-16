using System.Net;
using ExpertEase.Application.DataTransferObjects;
using ExpertEase.Application.DataTransferObjects.ServiceTaskDTOs;
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

public class ServiceTaskService(IRepository<WebAppDatabaseContext> repository,
    IConversationNotifier conversationNotifier,
    IStripeAccountService stripeAccountService,
    IReviewService reviewService): IServiceTaskService
{
    public async Task<ServiceResponse> CreateServiceTaskFromPayment(
        Guid paymentId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get payment details
            var payment = await repository.GetAsync(new PaymentSpec(paymentId), cancellationToken);
            if (payment == null)
                return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.NotFound, "Payment not found", ErrorCodes.EntityNotFound));

            // Get reply details
            var reply = await repository.GetAsync(new ReplySpec(payment.ReplyId), cancellationToken);
            if (reply == null)
                return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.NotFound, "Reply not found", ErrorCodes.EntityNotFound));

            var request = reply.Request;

            // Create service task
            var serviceTask = new ServiceTaskAddDto
            {
                UserId = request.SenderUserId,
                SpecialistId = request.ReceiverUserId,
                StartDate = reply.StartDate,
                EndDate = reply.EndDate,
                Description = request.Description,
                Address = request.Address,
                Price = reply.Price,
                PaymentId = paymentId,
            };

            var result = await AddServiceTask(serviceTask, cancellationToken);
        
            if (!result.IsSuccess)
                return ServiceResponse.CreateErrorResponse<ServiceTask>(result.Error);

            // Update payment with service task ID
            Console.WriteLine("Service Task Id: " + result.Result?.Id);
            payment.ServiceTaskId = result.Result?.Id;
            await repository.UpdateAsync(payment, cancellationToken);

            return ServiceResponse.CreateSuccessResponse();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating service task from payment: {ex.Message}");
            return ServiceResponse.CreateErrorResponse<ServiceTask>(new(HttpStatusCode.InternalServerError, "Service task creation failed", ErrorCodes.TechnicalError));
        }
    }
    
    public async Task<ServiceResponse<ServiceTask>> AddServiceTask(ServiceTaskAddDto service, CancellationToken cancellationToken = default)
    {
        var payment = await repository.GetAsync(new PaymentSpec(service.PaymentId), cancellationToken);
        if (payment == null)
        {
            return ServiceResponse.CreateErrorResponse<ServiceTask>(new (HttpStatusCode.NotFound, "Payment not found", ErrorCodes.EntityNotFound));
        }
        
        var user = await repository.GetAsync(new UserSpec(service.UserId), cancellationToken);

        if (user == null)
        {
            return ServiceResponse.CreateErrorResponse<ServiceTask>(new ErrorMessage(HttpStatusCode.NotFound, "User not found", ErrorCodes.EntityNotFound));
        }
        
        var specialist = await repository.GetAsync(new UserSpec(service.SpecialistId), cancellationToken);
        if (specialist == null)
        {
            return ServiceResponse.CreateErrorResponse<ServiceTask>(new ErrorMessage(HttpStatusCode.NotFound, "Specialist not found", ErrorCodes.EntityNotFound));
        }
        
        var serviceTask = new ServiceTask 
        {
            UserId = service.UserId,
            User = user,
            SpecialistId = service.SpecialistId,
            Specialist = specialist,
            PaymentId = service.PaymentId,
            Payment = payment,
            StartDate = service.StartDate,
            EndDate = service.EndDate,
            Address = service.Address,
            Description = service.Description,
            Price = service.Price,
            Status = JobStatusEnum.Confirmed,
        };
        
        await repository.AddAsync(serviceTask, cancellationToken);
        
        return ServiceResponse.CreateSuccessResponse(serviceTask);
    }
    
    public async Task<ServiceResponse<ServiceTaskDto>> GetServiceTask(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await repository.GetAsync(new ServiceTaskProjectionSpec(id), cancellationToken);
        
        return result != null ? 
            ServiceResponse.CreateSuccessResponse(result) : 
            ServiceResponse.CreateErrorResponse<ServiceTaskDto>(CommonErrors.EntityNotFound);
    }

    // ✅ Alternative approach: Create multiple specs for different query patterns
    public async Task<ServiceResponse<ServiceTaskDto>> GetCurrentServiceTask(Guid otherUserId, UserDto? requestingUser = null, CancellationToken cancellationToken = default)
    {
        if (requestingUser == null)
        {
            return ServiceResponse.CreateErrorResponse<ServiceTaskDto>(new(
                HttpStatusCode.Unauthorized,
                "User authentication required",
                ErrorCodes.Invalid));
        }

        ServiceTaskDto? result;

        // ✅ Try both role combinations to find the service task
        switch (requestingUser.Role)
        {
            case UserRoleEnum.Client:
                // Look for service task where current user is client and other user is specialist
                result = await repository.GetAsync(
                    new ServiceTaskProjectionSpec(requestingUser.Id, otherUserId), 
                    cancellationToken);
                break;
                
            case UserRoleEnum.Specialist:
                // Look for service task where current user is specialist and other user is client
                result = await repository.GetAsync(
                    new ServiceTaskProjectionSpec(otherUserId, requestingUser.Id), 
                    cancellationToken);
                break;
                
            default:
                return ServiceResponse.CreateErrorResponse<ServiceTaskDto>(new(
                    HttpStatusCode.BadRequest,
                    "Invalid user role"));
        }
        
        return result != null ? 
            ServiceResponse.CreateSuccessResponse(result) : 
            ServiceResponse.CreateErrorResponse<ServiceTaskDto>(CommonErrors.EntityNotFound);
    }
    
    public async Task<ServiceResponse<PagedResponse<ServiceTaskDto>>> GetServiceTasks(PaginationSearchQueryParams pagination, CancellationToken cancellationToken = default)
    {
        var result = await repository.PageAsync(pagination, new ServiceTaskProjectionSpec(pagination.Search), cancellationToken);
        
        return ServiceResponse.CreateSuccessResponse(result);
    }
    
    public async Task<ServiceResponse> UpdateServiceTask(ServiceTaskUpdateDto serviceTask, UserDto? requestingUser = null, CancellationToken cancellationToken = default)
    {
        var task = await repository.GetAsync(new ServiceTaskSpec(serviceTask.Id), cancellationToken);
        
        if (task == null)
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.NotFound, "Service task with this id not found!", ErrorCodes.EntityNotFound));
        
        task.StartDate = serviceTask.StartDate ?? task.StartDate;
        task.EndDate = serviceTask.EndDate ?? task.EndDate;
        task.Address = serviceTask.Address ?? task.Address;
        task.Price = serviceTask.Price ?? task.Price;

        await repository.UpdateAsync(task, cancellationToken);
        return ServiceResponse.CreateSuccessResponse();
    }
    
    public async Task<ServiceResponse> UpdateServiceTaskStatus(
        JobStatusUpdateDto serviceTask, 
        UserDto? requestingUser = null, 
        CancellationToken cancellationToken = default)
    {
        var task = await repository.GetAsync(new ServiceTaskSpec(serviceTask.Id), cancellationToken);
    
        if (task == null)
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.NotFound, "Service task with this id not found!", ErrorCodes.EntityNotFound));

        var oldStatus = task.Status;
        var newStatus = serviceTask.Status;

        // ✅ Validate status transitions
        if (!IsValidStatusTransition(oldStatus, newStatus))
        {
            return ServiceResponse.CreateErrorResponse(new(
                HttpStatusCode.BadRequest, 
                $"Invalid status transition from {oldStatus} to {newStatus}", 
                ErrorCodes.Invalid));
        }

        // Handle status transitions
        switch (newStatus)
        {
            case JobStatusEnum.Completed:
                var completionResult = await HandleServiceCompletion(task, cancellationToken);
                if (!completionResult.IsSuccess)
                {
                    return completionResult; // Don't update status if completion fails
                }
                break;
            
            case JobStatusEnum.Cancelled:
                await HandleServiceCancellation(task, cancellationToken);
                break;
            
            case JobStatusEnum.Reviewed:
                await HandleServiceReviewed(task, cancellationToken);
                break;
        }

        await repository.UpdateAsync(task, cancellationToken);
    
        // Send status change notifications
        await NotifyStatusChange(task, oldStatus, newStatus);
    
        return ServiceResponse.CreateSuccessResponse();
    }
    
    private static bool IsValidStatusTransition(JobStatusEnum currentStatus, JobStatusEnum newStatus)
    {
        return currentStatus switch
        {
            JobStatusEnum.Confirmed => newStatus is JobStatusEnum.Completed or JobStatusEnum.Cancelled,
            JobStatusEnum.Completed => newStatus is JobStatusEnum.Reviewed,
            JobStatusEnum.Cancelled => false, // Cannot transition from cancelled
            JobStatusEnum.Reviewed => false, // Cannot transition from reviewed
            _ => false
        };
    }

    // 🆕 Handle service completion (specialist clicks "Serviciu Finalizat")
    private async Task<ServiceResponse> HandleServiceCompletion(ServiceTask task, CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine($"✅ Processing service completion for task {task.Id}");
            
            // Validate task can be completed
            if (task.Status == JobStatusEnum.Completed)
            {
                Console.WriteLine($"⚠️ Service task {task.Id} is already completed");
                return ServiceResponse.CreateSuccessResponse(); // Already completed
            }

            if (task.Status == JobStatusEnum.Cancelled)
            {
                return ServiceResponse.CreateErrorResponse(new(
                    HttpStatusCode.BadRequest, 
                    "Cannot complete a cancelled service", 
                    ErrorCodes.Invalid));
            }

            // Step 1: Transfer money to specialist
            var transferResult = await TransferMoneyToSpecialist(task, cancellationToken);
            if (!transferResult.IsSuccess)
            {
                Console.WriteLine($"❌ Money transfer failed: {transferResult.Error?.Message}");
                return transferResult; // Don't complete service if transfer fails
            }

            // Step 2: Update task status
            task.Status = JobStatusEnum.Completed;
            task.CompletedAt = DateTime.UtcNow;
            task.TransferReference = transferResult.Result;
            
            Console.WriteLine($"✅ Service {task.Id} completed successfully with transfer {transferResult.Result}");
            
            // Step 3: Notify both parties about completion and review opportunity
            await NotifyBothPartiesForReviews(task);
            
            return ServiceResponse.CreateSuccessResponse();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error completing service {task.Id}: {ex.Message}");
            return ServiceResponse.CreateErrorResponse(new(
                HttpStatusCode.InternalServerError, 
                "Failed to complete service and transfer money", 
                ErrorCodes.TechnicalError));
        }
    }

    // 🆕 Handle service cancellation
    private async Task HandleServiceCancellation(ServiceTask task, CancellationToken cancellationToken)
    {
        task.Status = JobStatusEnum.Cancelled;
        task.CancelledAt = DateTime.UtcNow;
        
        Console.WriteLine($"❌ Service {task.Id} cancelled");

        // Process refund for cancelled services
        await ProcessCancellationRefund(task, cancellationToken);
        
        // Notify both parties about cancellation
        var cancellationPayload = new
        {
            TaskId = task.Id,
            Message = "Serviciul a fost anulat.",
            task.CancelledAt
        };
        
        await conversationNotifier.NotifyServiceStatusChanged(task.UserId, cancellationPayload);
        await conversationNotifier.NotifyServiceStatusChanged(task.SpecialistId, cancellationPayload);
    }

    // 🆕 Handle when service moves to reviewed state (both parties completed reviews)
    private async Task HandleServiceReviewed(ServiceTask task, CancellationToken cancellationToken)
    {
        task.Status = JobStatusEnum.Reviewed;
        task.ReviewedAt = DateTime.UtcNow;
        
        Console.WriteLine($"⭐ Service {task.Id} marked as reviewed - both parties left reviews");
        
        // Notify both parties that review process is complete
        var reviewCompletedPayload = new
        {
            TaskId = task.Id,
            Message = "Procesul de recenzie a fost finalizat. Mulțumim pentru feedback!",
            task.ReviewedAt
        };
        
        await conversationNotifier.NotifyServiceStatusChanged(task.UserId, reviewCompletedPayload);
        await conversationNotifier.NotifyServiceStatusChanged(task.SpecialistId, reviewCompletedPayload);
    }

    // 🆕 Transfer money to specialist
    private async Task<ServiceResponse<string>> TransferMoneyToSpecialist(ServiceTask task, CancellationToken cancellationToken)
    {
        try
        {
            // Get payment details
            var payment = await repository.GetAsync(new PaymentSpec(task.PaymentId), cancellationToken);
            if (payment == null)
            {
                return ServiceResponse.CreateErrorResponse<string>(new(
                    HttpStatusCode.NotFound, 
                    "Payment not found for this service task", 
                    ErrorCodes.EntityNotFound));
            }

            // Get specialist's Stripe account
            var specialist = await repository.GetAsync(new UserSpec(task.SpecialistId), cancellationToken);
            if (specialist?.SpecialistProfile?.StripeAccountId == null)
            {
                return ServiceResponse.CreateErrorResponse<string>(new(
                    HttpStatusCode.BadRequest, 
                    "Specialist doesn't have a Stripe account configured", 
                    ErrorCodes.Invalid));
            }

            Console.WriteLine($"💰 Transferring money for completed service:");
            Console.WriteLine($"   - Service Task: {task.Id}");
            Console.WriteLine($"   - Payment Intent: {payment.StripePaymentIntentId}");
            Console.WriteLine($"   - Service Amount: {task.Price} RON");
            Console.WriteLine($"   - Specialist Account: {specialist.SpecialistProfile.StripeAccountId}");

            // ✅ SIMPLIFIED: Just call transfer - StripeAccountService handles test funds automatically
            var transferResult = await stripeAccountService.TransferToSpecialist(
                payment.StripePaymentIntentId,
                specialist.SpecialistProfile.StripeAccountId,
                task.Price,
                $"Payment for completed service: {task.Description}"
            );

            if (transferResult.IsSuccess)
            {
                Console.WriteLine($"✅ Transfer successful: {transferResult.Result}");
                
                // Update payment record
                payment.TransferReference = transferResult.Result;
                payment.TransferredAt = DateTime.UtcNow;
                payment.IsTransferred = true;
                await repository.UpdateAsync(payment, cancellationToken);
            }
            else
            {
                Console.WriteLine($"❌ Transfer failed: {transferResult.Error?.Message}");
            }

            return transferResult;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Transfer error: {ex.Message}");
            return ServiceResponse.CreateErrorResponse<string>(new(
                HttpStatusCode.InternalServerError, 
                $"Transfer failed: {ex.Message}", 
                ErrorCodes.TechnicalError));
        }
    }

    // 🆕 Process refund for cancelled services
    private async Task ProcessCancellationRefund(ServiceTask task, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await repository.GetAsync(new PaymentSpec(task.PaymentId), cancellationToken);
            if (payment == null)
            {
                Console.WriteLine($"⚠️ Cannot refund: Payment not found for task {task.Id}");
                return;
            }

            if (payment.IsTransferred)
            {
                Console.WriteLine($"⚠️ Cannot refund: Payment already transferred to specialist for task {task.Id}");
                // Notify that refund is not possible
                await conversationNotifier.NotifyServiceStatusChanged(task.UserId, new
                {
                    TaskId = task.Id,
                    Message = "Serviciul a fost anulat, dar plata a fost deja transferată specialistului. Contactează suportul pentru asistență.",
                    RefundNotPossible = true
                });
                return;
            }

            Console.WriteLine($"💸 Processing refund for cancelled service {task.Id}");

            var refundResult = await stripeAccountService.RefundPayment(
                payment.StripePaymentIntentId,
                task.Price,
                $"Service cancelled: {task.Description}"
            );

            if (refundResult.IsSuccess)
            {
                Console.WriteLine($"✅ Refund successful: {refundResult.Result}");
                
                // Update payment record
                payment.RefundReference = refundResult.Result;
                payment.RefundedAt = DateTime.UtcNow;
                payment.IsRefunded = true;
                await repository.UpdateAsync(payment, cancellationToken);

                // Notify client about successful refund
                await conversationNotifier.NotifyServiceStatusChanged(task.UserId, new
                {
                    TaskId = task.Id,
                    Message = $"Serviciul a fost anulat și suma de {task.Price} RON va fi returnată în 5-10 zile lucrătoare.",
                    RefundAmount = task.Price,
                    RefundId = refundResult.Result,
                    RefundStatus = "processed"
                });
            }
            else
            {
                Console.WriteLine($"❌ Refund failed: {refundResult.Error?.Message}");
                
                // Notify about refund failure
                await conversationNotifier.NotifyServiceStatusChanged(task.UserId, new
                {
                    TaskId = task.Id,
                    Message = "Serviciul a fost anulat, dar a apărut o problemă cu returnarea banilor. Contactează suportul.",
                    RefundStatus = "failed",
                    Error = refundResult.Error?.Message
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Refund processing error: {ex.Message}");
            
            // Notify about refund error
            await conversationNotifier.NotifyServiceStatusChanged(task.UserId, new
            {
                TaskId = task.Id,
                Message = "Serviciul a fost anulat, dar a apărut o eroare în procesarea returnării. Contactează suportul.",
                RefundStatus = "error"
            });
        }
    }

    // 🆕 Notify both parties for reviews after completion
    private async Task NotifyBothPartiesForReviews(ServiceTask task)
    {
        Console.WriteLine($"🎯 Starting review notifications for service task {task.Id}");
        Console.WriteLine($"   - Client: {task.User.FullName} (ID: {task.UserId})");
        Console.WriteLine($"   - Specialist: {task.Specialist.FullName} (ID: {task.SpecialistId})");

        var reviewPayload = new
        {
            TaskId = task.Id,
            Message = "Serviciul a fost finalizat cu succes și plata a fost transferată! Poți lăsa o recenzie pentru a ajuta comunitatea.",
            ServiceDescription = task.Description,
            task.CompletedAt,
            TransferCompleted = !string.IsNullOrEmpty(task.TransferReference)
        };

        try
        {
            // Notify client - they can review the specialist
            Console.WriteLine($"📤 Sending service completion notification to CLIENT: {task.User.FullName}");
            await conversationNotifier.NotifyServiceCompleted(task.UserId, reviewPayload);
            
            Console.WriteLine($"📝 Sending review prompt to CLIENT to review SPECIALIST");
            await conversationNotifier.NotifyReviewPrompt(task.UserId, new
            {
                TaskId = task.Id,
                ReviewTargetId = task.SpecialistId,
                ReviewTargetName = task.Specialist.FullName,
                ReviewTargetRole = "specialist",
                Message = $"Poți lăsa o recenzie pentru specialistul {task.Specialist.FullName}!"
            });

            // Notify specialist - they can review the client
            Console.WriteLine($"📤 Sending service completion notification to SPECIALIST: {task.Specialist.FullName}");
            await conversationNotifier.NotifyServiceCompleted(task.SpecialistId, reviewPayload);
            
            Console.WriteLine($"📝 Sending review prompt to SPECIALIST to review CLIENT");
            await conversationNotifier.NotifyReviewPrompt(task.SpecialistId, new
            {
                TaskId = task.Id,
                ReviewTargetId = task.UserId,
                ReviewTargetName = task.User.FullName,
                ReviewTargetRole = "client",
                Message = $"Poți lăsa o recenzie pentru clientul {task.User.FullName}!"
            });

            Console.WriteLine($"✅ All review notifications sent successfully for service task {task.Id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error sending review notifications: {ex.Message}");
            Console.WriteLine($"   Stack trace: {ex.StackTrace}");
        }
    }

    // 🆕 Send status change notifications
    private async Task NotifyStatusChange(ServiceTask task, JobStatusEnum oldStatus, JobStatusEnum newStatus)
    {
        var statusPayload = new
        {
            TaskId = task.Id,
            OldStatus = oldStatus.ToString(),
            NewStatus = newStatus.ToString(),
            UpdatedAt = DateTime.UtcNow,
            HasMoneyTransfer = newStatus == JobStatusEnum.Completed
        };

        await conversationNotifier.NotifyServiceStatusChanged(task.UserId, statusPayload);
        await conversationNotifier.NotifyServiceStatusChanged(task.SpecialistId, statusPayload);
    }

    // 🆕 Method to be called when reviews are submitted
    public async Task<ServiceResponse> CheckAndUpdateReviewStatus(Guid taskId, CancellationToken cancellationToken = default)
    {
        try
        {
            var task = await repository.GetAsync(new ServiceTaskSpec(taskId), cancellationToken);
            if (task == null)
                return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.NotFound, "Service task not found", ErrorCodes.EntityNotFound));

            // Only update if task is currently completed (not already reviewed)
            if (task.Status != JobStatusEnum.Completed)
            {
                Console.WriteLine($"⚠️ Service task {taskId} is not in Completed status (current: {task.Status}), skipping review check");
                return ServiceResponse.CreateSuccessResponse();
            }

            // Check if both parties have submitted reviews
            var reviewsResponse = await reviewService.GetReviewsForServiceTask(taskId, cancellationToken);
            
            if (!reviewsResponse.IsSuccess || reviewsResponse.Result == null)
            {
                Console.WriteLine($"❌ Failed to get reviews for task {taskId}: {reviewsResponse.Error?.Message}");
                return ServiceResponse.CreateSuccessResponse(); // Don't fail the whole operation
            }

            var reviews = reviewsResponse.Result;
            Console.WriteLine($"📊 Found {reviews.Count} reviews for service task {taskId}");

            // 🆕 IMPORTANT: Check if both CLIENT and SPECIALIST have reviewed
            var clientReviewed = reviews.Any(r => r.SenderUserId == task.UserId);
            var specialistReviewed = reviews.Any(r => r.SenderUserId == task.SpecialistId);

            Console.WriteLine($"👤 Client ({task.User.FullName}) reviewed: {clientReviewed}");
            Console.WriteLine($"🔧 Specialist ({task.Specialist.FullName}) reviewed: {specialistReviewed}");

            // If both client and specialist have left reviews, mark as reviewed
            if (clientReviewed && specialistReviewed)
            {
                Console.WriteLine($"⭐ Both parties have reviewed service task {taskId} - updating to Reviewed status");
                
                task.Status = JobStatusEnum.Reviewed;
                task.ReviewedAt = DateTime.UtcNow;
                await repository.UpdateAsync(task, cancellationToken);
                
                await HandleServiceReviewed(task, cancellationToken);
            }
            else
            {
                Console.WriteLine($"📝 Waiting for more reviews - Client: {clientReviewed}, Specialist: {specialistReviewed}");
            }

            return ServiceResponse.CreateSuccessResponse();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error checking review status for task {taskId}: {ex.Message}");
            return ServiceResponse.CreateErrorResponse(new(
                HttpStatusCode.InternalServerError, 
                "Error checking review status", 
                ErrorCodes.TechnicalError));
        }
    }
    
    public async Task<ServiceResponse> DeleteServiceTask(Guid id, UserDto? requestingUser = null, CancellationToken cancellationToken = default)
    {
        var task = await repository.GetAsync(new ServiceTaskSpec(id), cancellationToken);
        
        if (task == null)
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.NotFound, "Service task with this id not found!", ErrorCodes.EntityNotFound));

        await repository.DeleteAsync<ServiceTask>(id, cancellationToken);
        return ServiceResponse.CreateSuccessResponse();
    }
}