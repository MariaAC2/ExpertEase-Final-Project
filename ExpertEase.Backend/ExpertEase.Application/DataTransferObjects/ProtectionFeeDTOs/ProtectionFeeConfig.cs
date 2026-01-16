namespace ExpertEase.Application.DataTransferObjects.ProtectionFeeDTOs;

public class ProtectionFeeConfig
{
    public string FeeType { get; set; } = "percentage"; // "percentage", "fixed", "hybrid"
    public decimal PercentageRate { get; set; } = 10.0m;
    public decimal FixedAmount { get; set; } = 25.0m;
    public decimal MinimumFee { get; set; } = 5.0m;
    public decimal MaximumFee { get; set; } = 100.0m;
    public bool IsEnabled { get; set; } = true;
}