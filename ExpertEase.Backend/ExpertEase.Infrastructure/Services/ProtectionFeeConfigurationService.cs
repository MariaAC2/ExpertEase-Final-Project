using ExpertEase.Application.DataTransferObjects.ProtectionFeeDTOs;
using ExpertEase.Application.Services;
using ExpertEase.Infrastructure.Configurations;
using ExpertEase.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExpertEase.Infrastructure.Services;

public class ProtectionFeeConfigurationService(
    IOptions<ProtectionFeeSettings> settings,
    ILogger<ProtectionFeeConfigurationService> logger)
    : IProtectionFeeConfigurationService
{
    private readonly ProtectionFeeSettings _settings = settings.Value;

    /// <summary>
    /// Get current protection fee configuration
    /// </summary>
    public ProtectionFeeConfig GetCurrentConfiguration()
    {
        return _settings.ToProtectionFeeConfig();
    }

    /// <summary>
    /// Calculate protection fee for a service amount
    /// </summary>
    public ProtectionFeeCalculation CalculateProtectionFee(decimal serviceAmount)
    {
        var config = GetCurrentConfiguration();
        return serviceAmount.CalculateProtectionFee(config);
    }

    /// <summary>
    /// Calculate complete payment breakdown
    /// </summary>
    public PaymentAmountBreakdown CalculatePaymentBreakdown(decimal serviceAmount)
    {
        var config = GetCurrentConfiguration();
        return serviceAmount.CalculateTotalAmount(config);
    }

    /// <summary>
    /// Validate that current configuration is valid
    /// </summary>
    public Task<bool> ValidateConfigurationAsync()
    {
        try
        {
            var config = GetCurrentConfiguration();
            
            // Validate configuration
            if (config.PercentageRate < 0 || config.PercentageRate > 100)
            {
                logger.LogError("Invalid percentage rate: {Rate}. Must be between 0 and 100.", config.PercentageRate);
                return Task.FromResult(false);
            }

            if (config.MinimumFee < 0)
            {
                logger.LogError("Invalid minimum fee: {MinFee}. Must be >= 0.", config.MinimumFee);
                return Task.FromResult(false);
            }

            if (config.MaximumFee < config.MinimumFee)
            {
                logger.LogError("Invalid fee range: Min={MinFee}, Max={MaxFee}. Max must be >= Min.", 
                    config.MinimumFee, config.MaximumFee);
                return Task.FromResult(false);
            }

            logger.LogInformation("✅ Protection fee configuration validated successfully");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error validating protection fee configuration");
            return Task.FromResult(false);
        }
    }
}