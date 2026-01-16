using ExpertEase.Application.DataTransferObjects.ProtectionFeeDTOs;

namespace ExpertEase.Infrastructure.Configurations;

public class ProtectionFeeSettings
{
    public const string SectionName = "ProtectionFee";
    
    public string FeeType { get; set; } = "percentage";
    public decimal PercentageRate { get; set; } = 10.0m;
    public decimal FixedAmount { get; set; } = 25.0m;
    public decimal MinimumFee { get; set; } = 5.0m;
    public decimal MaximumFee { get; set; } = 100.0m;
    public bool IsEnabled { get; set; } = true;
    public string Description { get; set; } = "Client protection fee";
    
    /// <summary>
    /// Convert to ProtectionFeeConfig for use with extension methods
    /// </summary>
    public ProtectionFeeConfig ToProtectionFeeConfig()
    {
        return new ProtectionFeeConfig
        {
            FeeType = FeeType,
            PercentageRate = PercentageRate,
            FixedAmount = FixedAmount,
            MinimumFee = MinimumFee,
            MaximumFee = MaximumFee,
            IsEnabled = IsEnabled
        };
    }
}