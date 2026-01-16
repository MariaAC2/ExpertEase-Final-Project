using ExpertEase.Domain.Entities;
using ExpertEase.Domain.Enums;
using ExpertEase.Application.DataTransferObjects.PaymentDTOs;
using ExpertEase.Application.DataTransferObjects.ProtectionFeeDTOs;

namespace ExpertEase.Infrastructure.Extensions;

/// <summary>
/// Extension methods for Payment entity with full escrow support
/// Provides business logic for payment states, validations, and calculations
/// </summary>
public static class PaymentExtensions
{
    #region Currency Conversion

    /// <summary>
    /// Converts amount from RON to smallest currency unit (bani) for Stripe
    /// </summary>
    /// <param name="amount">Amount in RON</param>
    /// <returns>Amount in bani (cents)</returns>
    public static long ToStripeAmount(this decimal amount)
    {
        return (long)(amount * 100);
    }

    /// <summary>
    /// Converts amount from smallest currency unit (bani) to RON
    /// </summary>
    /// <param name="amount">Amount in bani (cents)</param>
    /// <returns>Amount in RON</returns>
    public static decimal FromStripeAmount(this long amount)
    {
        return amount / 100m;
    }

    #endregion

    #region Payment State Validation

    /// <summary>
    /// ✅ ENHANCED: Checks if payment can be refunded (supports escrow and released payments)
    /// </summary>
    /// <param name="payment">Payment to check</param>
    /// <returns>True if payment can be refunded</returns>
    public static bool CanBeRefunded(this Payment payment)
    {
        // Valid statuses for refunding
        var refundableStatuses = new[] 
        { 
            PaymentStatusEnum.Completed,    // Legacy status
            PaymentStatusEnum.Escrowed,     // Money held in escrow
            PaymentStatusEnum.Released      // Money already sent to specialist
        };
        
        if (!refundableStatuses.Contains(payment.Status))
            return false;

        // Cannot refund if already fully refunded
        if (payment.RefundedAmount >= payment.TotalAmount)
            return false;

        // Must have been paid
        if (!payment.PaidAt.HasValue)
            return false;

        // 30-day refund window from payment date
        var refundDeadline = payment.PaidAt.Value.AddDays(30);
        if (DateTime.UtcNow > refundDeadline)
            return false;

        return true;
    }

    /// <summary>
    /// ✅ NEW: Checks if payment can be released to specialist (escrow → specialist)
    /// </summary>
    /// <param name="payment">Payment to check</param>
    /// <returns>True if payment can be released</returns>
    public static bool CanBeReleased(this Payment payment)
    {
        // Must be in escrow status (money held on platform)
        var validStatuses = new[] 
        { 
            PaymentStatusEnum.Completed,    // Legacy - treat as escrowed
            PaymentStatusEnum.Escrowed      // Explicitly escrowed
        };
        
        if (!validStatuses.Contains(payment.Status))
            return false;

        // Cannot release if already transferred
        if (payment.TransferredAmount > 0)
            return false;

        // Must have service amount to transfer
        if (payment.ServiceAmount <= 0)
            return false;

        // Payment must be confirmed (money received)
        if (!payment.PaidAt.HasValue)
            return false;

        return true;
    }

    /// <summary>
    /// Checks if payment can be cancelled (only pending payments)
    /// </summary>
    /// <param name="payment">Payment to check</param>
    /// <returns>True if payment can be cancelled</returns>
    public static bool CanBeCancelled(this Payment payment)
    {
        return payment.Status == PaymentStatusEnum.Pending || 
               payment.Status == PaymentStatusEnum.Processing;
    }

    /// <summary>
    /// ✅ NEW: Checks if payment is currently held in escrow
    /// </summary>
    /// <param name="payment">Payment to check</param>
    /// <returns>True if payment is in escrow</returns>
    public static bool IsInEscrow(this Payment payment)
    {
        var escrowStatuses = new[] 
        { 
            PaymentStatusEnum.Completed,    // Legacy - treat as escrowed
            PaymentStatusEnum.Escrowed      // Explicitly escrowed
        };
        
        return escrowStatuses.Contains(payment.Status) &&
               payment.TransferredAmount == 0 &&
               payment.RefundedAmount < payment.TotalAmount &&
               payment.PaidAt.HasValue;
    }

    /// <summary>
    /// ✅ NEW: Checks if payment has been fully processed (no pending actions)
    /// </summary>
    /// <param name="payment">Payment to check</param>
    /// <returns>True if payment is fully processed</returns>
    public static bool IsFullyProcessed(this Payment payment)
    {
        return payment.Status == PaymentStatusEnum.Released ||
               payment.Status == PaymentStatusEnum.Refunded ||
               payment.Status == PaymentStatusEnum.Cancelled;
    }

    #endregion

    #region Amount Calculations

    /// <summary>
    /// ✅ NEW: Gets the amount currently held in escrow
    /// </summary>
    /// <param name="payment">Payment to analyze</param>
    /// <returns>Amount held in escrow</returns>
    public static decimal GetEscrowedAmount(this Payment payment)
    {
        if (!payment.IsInEscrow())
            return 0;

        return payment.TotalAmount - payment.TransferredAmount - payment.RefundedAmount;
    }

    /// <summary>
    /// ✅ NEW: Gets the maximum amount that can be refunded
    /// </summary>
    /// <param name="payment">Payment to analyze</param>
    /// <returns>Maximum refundable amount</returns>
    public static decimal GetMaxRefundableAmount(this Payment payment)
    {
        if (!payment.CanBeRefunded())
            return 0;

        return payment.TotalAmount - payment.RefundedAmount;
    }

    /// <summary>
    /// ✅ NEW: Gets the amount available for release to specialist
    /// </summary>
    /// <param name="payment">Payment to analyze</param>
    /// <returns>Amount that can be released to specialist</returns>
    public static decimal GetReleasableAmount(this Payment payment)
    {
        if (!payment.CanBeReleased())
            return 0;

        // Only service amount can be released (platform keeps the fee)
        return payment.ServiceAmount;
    }

    /// <summary>
    /// ✅ NEW: Gets the platform's net revenue from this payment
    /// </summary>
    /// <param name="payment">Payment to analyze</param>
    /// <returns>Platform's confirmed revenue</returns>
    public static decimal GetPlatformRevenue(this Payment payment)
    {
        if (!payment.FeeCollected)
            return 0;

        // Platform revenue is the protection fee minus any refunded portion of the fee
        var feeRefunded = Math.Min(payment.RefundedAmount, payment.ProtectionFee);
        return payment.ProtectionFee - feeRefunded;
    }

    /// <summary>
    /// ✅ NEW: Gets specialist's net earnings from this payment
    /// </summary>
    /// <param name="payment">Payment to analyze</param>
    /// <returns>Amount specialist received or will receive</returns>
    public static decimal GetSpecialistEarnings(this Payment payment)
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

    #endregion

    #region Status and Display

    /// <summary>
    /// ✅ UPDATED: Enhanced status messages with escrow states
    /// </summary>
    /// <param name="status">Payment status enum</param>
    /// <returns>User-friendly status message in Romanian</returns>
    public static string GetStatusMessage(this PaymentStatusEnum status)
    {
        return status switch
        {
            PaymentStatusEnum.Pending => "În așteptare",
            PaymentStatusEnum.Processing => "Se procesează",
            PaymentStatusEnum.Completed => "Finalizată - În siguranță",
            PaymentStatusEnum.Escrowed => "În siguranță",
            PaymentStatusEnum.Released => "Transferată către specialist",
            PaymentStatusEnum.Failed => "Eșuată",
            PaymentStatusEnum.Cancelled => "Anulată",
            PaymentStatusEnum.Refunded => "Rambursată",
            PaymentStatusEnum.Disputed => "În dispută",
            _ => "Status necunoscut"
        };
    }

    /// <summary>
    /// ✅ NEW: Get detailed payment flow description for user communication
    /// </summary>
    /// <param name="payment">Payment to describe</param>
    /// <returns>Detailed description of payment state</returns>
    public static string GetPaymentFlowDescription(this Payment payment)
    {
        return payment.Status switch
        {
            PaymentStatusEnum.Pending => 
                "Plata este în așteptare. Vă rugăm să finalizați procesul de plată.",
            
            PaymentStatusEnum.Processing => 
                "Plata se procesează. Vă rugăm să așteptați...",
            
            PaymentStatusEnum.Completed or PaymentStatusEnum.Escrowed => 
                payment.TransferredAmount == 0 
                    ? "Plata a fost realizată cu succes și este ținută în siguranță până la finalizarea serviciului."
                    : "Plata a fost realizată și transferată către specialist.",
            
            PaymentStatusEnum.Released => 
                $"Plata a fost transferată către specialist după finalizarea serviciului. Suma transferată: {payment.TransferredAmount:F2} RON.",
            
            PaymentStatusEnum.Refunded => 
                $"Plata a fost rambursată. Suma de {payment.RefundedAmount:F2} RON va fi returnată în 3-5 zile lucrătoare.",
            
            PaymentStatusEnum.Failed => 
                "Plata a eșuat. Vă rugăm să încercați din nou sau să folosiți o altă metodă de plată.",
            
            PaymentStatusEnum.Cancelled => 
                "Plata a fost anulată.",
            
            PaymentStatusEnum.Disputed => 
                "Plata este în dispută. Echipa noastră vă va contacta în curând.",
            
            _ => "Status necunoscut al plății."
        };
    }

    /// <summary>
    /// ✅ NEW: Get payment status color for UI display
    /// </summary>
    /// <param name="status">Payment status</param>
    /// <returns>Color class or hex code for UI</returns>
    public static string GetStatusColor(this PaymentStatusEnum status)
    {
        return status switch
        {
            PaymentStatusEnum.Pending => "#FFA500",        // Orange
            PaymentStatusEnum.Processing => "#2196F3",     // Blue
            PaymentStatusEnum.Completed => "#4CAF50",      // Green
            PaymentStatusEnum.Escrowed => "#4CAF50",       // Green
            PaymentStatusEnum.Released => "#8BC34A",       // Light Green
            PaymentStatusEnum.Failed => "#F44336",         // Red
            PaymentStatusEnum.Cancelled => "#9E9E9E",      // Gray
            PaymentStatusEnum.Refunded => "#FF9800",       // Amber
            PaymentStatusEnum.Disputed => "#E91E63",       // Pink
            _ => "#9E9E9E"                                 // Default Gray
        };
    }

    #endregion

    #region Protection Fee Calculations

    /// <summary>
    /// ✅ UPDATED: Enhanced protection fee calculation with configurable options
    /// </summary>
    /// <param name="serviceAmount">Base service amount</param>
    /// <param name="config">Fee configuration (null for default)</param>
    /// <returns>Detailed fee calculation result</returns>
    public static ProtectionFeeCalculation CalculateProtectionFee(
        this decimal serviceAmount, 
        ProtectionFeeConfig? config = null)
    {
        // Use default config if none provided
        config ??= GetDefaultProtectionFeeConfig();

        if (!config.IsEnabled)
        {
            return new ProtectionFeeCalculation
            {
                BaseAmount = serviceAmount,
                FeeType = "disabled",
                CalculatedFee = 0,
                FinalFee = 0,
                Justification = "Protection fee is disabled"
            };
        }

        decimal calculatedFee;
        string justification;

        switch (config.FeeType.ToLower())
        {
            case "percentage":
                calculatedFee = serviceAmount * (config.PercentageRate / 100m);
                justification = $"{config.PercentageRate}% of service amount";
                break;
                
            case "fixed":
                calculatedFee = config.FixedAmount;
                justification = $"Fixed fee of {config.FixedAmount} RON";
                break;
                
            case "hybrid":
                var percentageFee = serviceAmount * (config.PercentageRate / 100m);
                calculatedFee = Math.Max(percentageFee, config.FixedAmount);
                justification = $"Higher of {config.PercentageRate}% ({percentageFee:F2} RON) or fixed {config.FixedAmount} RON";
                break;
                
            default:
                calculatedFee = config.FixedAmount;
                justification = $"Default fixed fee of {config.FixedAmount} RON";
                break;
        }

        // Apply min/max limits
        var originalFee = calculatedFee;
        calculatedFee = Math.Max(config.MinimumFee, calculatedFee);
        calculatedFee = Math.Min(config.MaximumFee, calculatedFee);

        if (calculatedFee != originalFee)
        {
            if (calculatedFee == config.MinimumFee)
                justification += $" (minimum {config.MinimumFee} RON applied)";
            else if (calculatedFee == config.MaximumFee)
                justification += $" (maximum {config.MaximumFee} RON applied)";
        }

        return new ProtectionFeeCalculation
        {
            BaseAmount = serviceAmount,
            FeeType = config.FeeType,
            PercentageRate = config.PercentageRate,
            FixedAmount = config.FixedAmount,
            MinimumFee = config.MinimumFee,
            MaximumFee = config.MaximumFee,
            CalculatedFee = originalFee,
            FinalFee = calculatedFee,
            Justification = justification
        };
    }

    /// <summary>
    /// ✅ UPDATED: Calculate total amount with detailed breakdown
    /// </summary>
    /// <param name="serviceAmount">Base service amount</param>
    /// <param name="config">Fee configuration (null for default)</param>
    /// <returns>Complete payment breakdown</returns>
    public static PaymentAmountBreakdown CalculateTotalAmount(
        this decimal serviceAmount, 
        ProtectionFeeConfig? config = null)
    {
        var feeCalculation = serviceAmount.CalculateProtectionFee(config);
        
        return new PaymentAmountBreakdown
        {
            ServiceAmount = serviceAmount,
            ProtectionFee = feeCalculation.FinalFee,
            TotalAmount = serviceAmount + feeCalculation.FinalFee,
            ProtectionFeeCalculation = feeCalculation
        };
    }

    #endregion

    #region DTO Conversion Helpers

    /// <summary>
    /// ✅ NEW: Convert payment to status response DTO
    /// Note: For full DTO conversion with serialization, use PaymentHelpers.ToStatusResponseDTO()
    /// </summary>
    /// <param name="payment">Payment entity</param>
    /// <returns>Payment status DTO</returns>
    public static PaymentStatusResponseDto ToStatusResponseDto(this Payment payment)
    {
        return new PaymentStatusResponseDto
        {
            PaymentId = payment.Id,
            ServiceTaskId = payment.ServiceTaskId ?? null,
            Status = payment.Status.GetStatusMessage(),
            IsEscrowed = payment.IsInEscrow(),
            CanBeReleased = payment.CanBeReleased(),
            CanBeRefunded = payment.CanBeRefunded(),
            AmountBreakdown = new PaymentAmountBreakdown
            {
                ServiceAmount = payment.ServiceAmount,
                ProtectionFee = payment.ProtectionFee,
                TotalAmount = payment.TotalAmount,
                TransferredAmount = payment.TransferredAmount,
                RefundedAmount = payment.RefundedAmount,
                PendingAmount = payment.GetEscrowedAmount(),
                PlatformRevenue = payment.GetPlatformRevenue()
            },
            ProtectionFeeDetails = null // Use PaymentHelpers.GetProtectionFeeDetails() for this
        };
    }

    #endregion

    #region Validation Helpers

    /// <summary>
    /// ✅ NEW: Validate payment amounts consistency
    /// </summary>
    /// <param name="payment">Payment to validate</param>
    /// <returns>Validation result</returns>
    public static (bool IsValid, string? ErrorMessage) ValidateAmounts(this Payment payment)
    {
        if (payment.ServiceAmount < 0)
            return (false, "Service amount cannot be negative");

        if (payment.ProtectionFee < 0)
            return (false, "Protection fee cannot be negative");

        var expectedTotal = payment.ServiceAmount + payment.ProtectionFee;
        if (Math.Abs(payment.TotalAmount - expectedTotal) > 0.01m)
            return (false, $"Total amount mismatch. Expected: {expectedTotal}, Actual: {payment.TotalAmount}");

        if (payment.TransferredAmount > payment.ServiceAmount)
            return (false, "Transferred amount cannot exceed service amount");

        if (payment.RefundedAmount > payment.TotalAmount)
            return (false, "Refunded amount cannot exceed total amount");

        return (true, null);
    }

    /// <summary>
    /// ✅ NEW: Validate payment state transition
    /// </summary>
    /// <param name="payment">Payment entity</param>
    /// <param name="newStatus">Target status</param>
    /// <returns>True if transition is valid</returns>
    public static bool CanTransitionTo(this Payment payment, PaymentStatusEnum newStatus)
    {
        return payment.Status switch
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

    #region Private Helper Methods

    /// <summary>
    /// Get default protection fee configuration
    /// </summary>
    private static ProtectionFeeConfig GetDefaultProtectionFeeConfig()
    {
        return new ProtectionFeeConfig
        {
            FeeType = "percentage",
            PercentageRate = 10.0m,  // 10%
            FixedAmount = 25.0m,     // 25 RON fallback
            MinimumFee = 5.0m,       // Minimum 5 RON
            MaximumFee = 100.0m,     // Maximum 100 RON
            IsEnabled = true
        };
    }

    #endregion
}