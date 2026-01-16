using ExpertEase.Application.DataTransferObjects.PaymentDTOs;
using ExpertEase.Domain.Entities;
using ExpertEase.Infrastructure.Extensions;
using System.Text.Json;
using ExpertEase.Application.DataTransferObjects.ProtectionFeeDTOs;
using ExpertEase.Domain.Enums;

namespace ExpertEase.Infrastructure.Extensions;

/// <summary>
/// Helper class for Payment entity operations that involve DTOs
/// Handles serialization/deserialization between domain entities and application DTOs
/// </summary>
public static class PaymentHelpers
{
    #region Protection Fee Details Handling

    /// <summary>
    /// Get protection fee details from payment entity as DTO
    /// </summary>
    /// <param name="payment">Payment entity</param>
    /// <returns>Protection fee details DTO or null</returns>
    public static ProtectionFeeDetailsDto? GetProtectionFeeDetails(Payment payment)
    {
        if (string.IsNullOrWhiteSpace(payment.ProtectionFeeDetailsJson))
            return null;
            
        try
        {
            return JsonSerializer.Deserialize<ProtectionFeeDetailsDto>(payment.ProtectionFeeDetailsJson);
        }
        catch (JsonException ex)
        {
            // Log the error if you have logging available
            // _logger.LogWarning("Failed to deserialize protection fee details for payment {PaymentId}: {Error}", payment.Id, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Set protection fee details on payment entity from DTO
    /// </summary>
    /// <param name="payment">Payment entity</param>
    /// <param name="details">Protection fee details DTO</param>
    public static void SetProtectionFeeDetails(Payment payment, ProtectionFeeDetailsDto details)
    {
        if (details == null)
        {
            payment.ProtectionFeeDetailsJson = null;
            return;
        }

        try
        {
            payment.ProtectionFeeDetailsJson = JsonSerializer.Serialize(details, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
        }
        catch (JsonException ex)
        {
            // Log the error if you have logging available
            // _logger.LogError("Failed to serialize protection fee details for payment {PaymentId}: {Error}", payment.Id, ex.Message);
            throw new InvalidOperationException("Failed to serialize protection fee details", ex);
        }
    }

    #endregion

    #region Payment Conversion Helpers

    /// <summary>
    /// Convert Payment entity to PaymentStatusResponseDTO
    /// </summary>
    /// <param name="payment">Payment entity</param>
    /// <returns>Payment status response DTO</returns>
    public static PaymentStatusResponseDto ToStatusResponseDto(Payment payment)
    {
        return new PaymentStatusResponseDto
        {
            PaymentId = payment.Id,
            ServiceTaskId = payment.ServiceTaskId ?? null,
            Status = payment.Status.GetStatusMessage(),
            IsEscrowed = payment.IsInEscrow(),
            CanBeReleased = payment.CanBeReleased(),
            CanBeRefunded = payment.CanBeRefunded(),
            AmountBreakdown = CreateAmountBreakdown(payment),
            ProtectionFeeDetails = GetProtectionFeeDetails(payment)
        };
    }

    /// <summary>
    /// Convert Payment entity to PaymentDetailsDTO
    /// </summary>
    /// <param name="payment">Payment entity</param>
    /// <param name="reply">Associated reply entity</param>
    /// <returns>Payment details DTO</returns>
    public static PaymentDetailsDto ToPaymentDetailsDto(Payment payment, Reply reply)
    {
        return new PaymentDetailsDto
        {
            Id = payment.Id,
            ReplyId = payment.ReplyId,
            ServiceAmount = payment.ServiceAmount,
            ProtectionFee = payment.ProtectionFee,
            TotalAmount = payment.TotalAmount,
            Currency = payment.Currency ?? "RON",
            Status = payment.Status.GetStatusMessage(),
            PaidAt = payment.PaidAt,
            EscrowReleasedAt = payment.EscrowReleasedAt,
            CreatedAt = payment.CreatedAt,
            StripePaymentIntentId = payment.StripePaymentIntentId,
            StripeTransferId = payment.StripeTransferId,
            StripeRefundId = payment.StripeRefundId,
            ServiceDescription = reply.Request.Description,
            ServiceAddress = reply.Request.Address,
            ServiceStartDate = reply.StartDate,
            ServiceEndDate = reply.EndDate,
            SpecialistName = reply.Request.ReceiverUser.FullName,
            ClientName = reply.Request.SenderUser.FullName,
            TransferredAmount = payment.TransferredAmount,
            RefundedAmount = payment.RefundedAmount,
            PlatformRevenue = payment.GetPlatformRevenue(),
            IsEscrowed = payment.IsInEscrow(),
            ProtectionFeeDetails = GetProtectionFeeDetails(payment)
        };
    }

    /// <summary>
    /// Convert Payment entity to PaymentHistoryDTO
    /// </summary>
    /// <param name="payment">Payment entity</param>
    /// <param name="reply">Associated reply entity</param>
    /// <returns>Payment history DTO</returns>
    public static PaymentHistoryDto ToPaymentHistoryDto(Payment payment, Reply reply)
    {
        return new PaymentHistoryDto
        {
            Id = payment.Id,
            ReplyId = payment.ReplyId,
            ServiceAmount = payment.ServiceAmount,
            ProtectionFee = payment.ProtectionFee,
            TotalAmount = payment.TotalAmount,
            Currency = payment.Currency ?? "RON",
            Status = payment.Status.GetStatusMessage(),
            PaidAt = payment.PaidAt,
            EscrowReleasedAt = payment.EscrowReleasedAt,
            ServiceDescription = reply.Request.Description,
            ServiceAddress = reply.Request.Address,
            SpecialistName = reply.Request.ReceiverUser.FullName,
            ClientName = reply.Request.SenderUser.FullName,
            TransferredAmount = payment.TransferredAmount,
            RefundedAmount = payment.RefundedAmount,
            IsEscrowed = payment.IsInEscrow()
        };
    }

    #endregion

    #region Validation Helpers

    /// <summary>
    /// Validate payment amounts for consistency
    /// </summary>
    /// <param name="payment">Payment to validate</param>
    /// <returns>Validation result</returns>
    public static (bool IsValid, string? ErrorMessage) ValidatePaymentAmounts(Payment payment)
    {
        if (payment.ServiceAmount < 0)
            return (false, "Service amount cannot be negative");

        if (payment.ProtectionFee < 0)
            return (false, "Protection fee cannot be negative");

        var expectedTotal = payment.ServiceAmount + payment.ProtectionFee;
        if (Math.Abs(payment.TotalAmount - expectedTotal) > 0.01m)
            return (false, $"Total amount mismatch. Expected: {expectedTotal:F2}, Actual: {payment.TotalAmount:F2}");

        if (payment.TransferredAmount > payment.ServiceAmount)
            return (false, "Transferred amount cannot exceed service amount");

        if (payment.RefundedAmount > payment.TotalAmount)
            return (false, "Refunded amount cannot exceed total amount");

        if (payment.TransferredAmount < 0)
            return (false, "Transferred amount cannot be negative");

        if (payment.RefundedAmount < 0)
            return (false, "Refunded amount cannot be negative");

        return (true, null);
    }

    /// <summary>
    /// Validate payment state transition
    /// </summary>
    /// <param name="currentStatus">Current payment status</param>
    /// <param name="newStatus">Target status</param>
    /// <returns>True if transition is valid</returns>
    public static bool IsValidStatusTransition(PaymentStatusEnum currentStatus, PaymentStatusEnum newStatus)
    {
        return currentStatus switch
        {
            PaymentStatusEnum.Pending => newStatus is PaymentStatusEnum.Processing or 
                                                      PaymentStatusEnum.Completed or 
                                                      PaymentStatusEnum.Escrowed or 
                                                      PaymentStatusEnum.Failed or 
                                                      PaymentStatusEnum.Cancelled,

            PaymentStatusEnum.Processing => newStatus is PaymentStatusEnum.Completed or 
                                                         PaymentStatusEnum.Escrowed or 
                                                         PaymentStatusEnum.Failed or 
                                                         PaymentStatusEnum.Cancelled,

            PaymentStatusEnum.Completed or PaymentStatusEnum.Escrowed => 
                                           newStatus is PaymentStatusEnum.Released or 
                                                        PaymentStatusEnum.Refunded or 
                                                        PaymentStatusEnum.Disputed,

            PaymentStatusEnum.Released => newStatus is PaymentStatusEnum.Refunded or 
                                                       PaymentStatusEnum.Disputed,

            // Terminal states - no transitions allowed
            PaymentStatusEnum.Failed or 
            PaymentStatusEnum.Cancelled or 
            PaymentStatusEnum.Refunded => false,

            PaymentStatusEnum.Disputed => newStatus is PaymentStatusEnum.Released or 
                                                       PaymentStatusEnum.Refunded,

            _ => false
        };
    }

    #endregion

    #region Business Logic Helpers

    /// <summary>
    /// Calculate platform revenue from payment
    /// </summary>
    /// <param name="payment">Payment entity</param>
    /// <returns>Platform's confirmed revenue</returns>
    public static decimal CalculatePlatformRevenue(Payment payment)
    {
        if (!payment.FeeCollected)
            return 0;

        // Platform revenue is the protection fee minus any refunded portion of the fee
        var feeRefunded = Math.Min(payment.RefundedAmount, payment.ProtectionFee);
        return payment.ProtectionFee - feeRefunded;
    }

    /// <summary>
    /// Calculate specialist's net earnings
    /// </summary>
    /// <param name="payment">Payment entity</param>
    /// <returns>Amount specialist received or will receive</returns>
    public static decimal CalculateSpecialistEarnings(Payment payment)
    {
        if (payment.Status == PaymentStatusEnum.Released)
        {
            return payment.TransferredAmount;
        }
        
        if (payment.IsInEscrow())
        {
            // Potential earnings (not yet transferred)
            var serviceRefunded = Math.Max(0, payment.RefundedAmount - payment.ProtectionFee);
            return Math.Max(0, payment.ServiceAmount - serviceRefunded);
        }

        return 0;
    }

    /// <summary>
    /// Get amount currently held in escrow
    /// </summary>
    /// <param name="payment">Payment entity</param>
    /// <returns>Amount held in escrow</returns>
    public static decimal GetEscrowedAmount(Payment payment)
    {
        if (!payment.IsInEscrow())
            return 0;

        return payment.TotalAmount - payment.TransferredAmount - payment.RefundedAmount;
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Create payment amount breakdown
    /// </summary>
    /// <param name="payment">Payment entity</param>
    /// <returns>Amount breakdown DTO</returns>
    private static PaymentAmountBreakdown CreateAmountBreakdown(Payment payment)
    {
        return new PaymentAmountBreakdown
        {
            ServiceAmount = payment.ServiceAmount,
            ProtectionFee = payment.ProtectionFee,
            TotalAmount = payment.TotalAmount,
            TransferredAmount = payment.TransferredAmount,
            RefundedAmount = payment.RefundedAmount,
            PendingAmount = GetEscrowedAmount(payment),
            PlatformRevenue = CalculatePlatformRevenue(payment)
        };
    }

    #endregion
}