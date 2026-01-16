namespace ExpertEase.Application.DataTransferObjects.ProtectionFeeDTOs;

public record ProtectionFeeCalculation
{
    public decimal BaseAmount { get; init; }
    public string FeeType { get; init; } = string.Empty;
    public decimal PercentageRate { get; init; }
    public decimal FixedAmount { get; init; }
    public decimal MinimumFee { get; init; }
    public decimal MaximumFee { get; init; }
    public decimal CalculatedFee { get; init; }
    public decimal FinalFee { get; init; }
    public string Justification { get; init; } = string.Empty;
}