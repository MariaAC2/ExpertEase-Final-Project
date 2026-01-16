using ExpertEase.Application.DataTransferObjects.PaymentDTOs;
using ExpertEase.Application.Requests;
using ExpertEase.Application.Responses;
using ExpertEase.Domain.Entities;

namespace ExpertEase.Application.Services;

public interface IPaymentService
{
    // Existing methods
    Task<ServiceResponse<PaymentIntentResponseDto>> CreatePaymentIntent(PaymentIntentAddDto addDto, CancellationToken cancellationToken = default);
    Task<ServiceResponse> ConfirmPayment(PaymentConfirmationDto confirmationDto, CancellationToken cancellationToken = default);
    
    // ✅ NEW: Add these escrow methods
    Task<ServiceResponse> ReleasePayment(PaymentReleaseDto releaseDto, CancellationToken cancellationToken = default);
    Task<ServiceResponse<PaymentStatusResponseDto>> GetPaymentStatus(Guid paymentId, CancellationToken cancellationToken = default);
    Task<ServiceResponse<PaymentReportDto>> GetRevenueReport(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    
    // Existing methods
    Task<ServiceResponse<PagedResponse<PaymentHistoryDto>>> GetPaymentHistory(Guid userId, PaginationSearchQueryParams pagination, CancellationToken cancellationToken = default);
    Task<ServiceResponse<PaymentDetailsDto>> GetPaymentDetails(Guid paymentId, CancellationToken cancellationToken = default);
    Task<ServiceResponse> RefundPayment(PaymentRefundDto refundDto, CancellationToken cancellationToken = default);
    Task<ServiceResponse> CancelPayment(Guid paymentId, CancellationToken cancellationToken = default);
    Task<ServiceResponse> HandleStripeWebhook(string eventJson, string signature, CancellationToken cancellationToken = default);
}