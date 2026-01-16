using ExpertEase.Application.DataTransferObjects.ProtectionFeeDTOs;

namespace ExpertEase.Application.Services;

public interface IProtectionFeeConfigurationService
{
    ProtectionFeeConfig GetCurrentConfiguration();
    ProtectionFeeCalculation CalculateProtectionFee(decimal serviceAmount);
    PaymentAmountBreakdown CalculatePaymentBreakdown(decimal serviceAmount);
    Task<bool> ValidateConfigurationAsync();
}