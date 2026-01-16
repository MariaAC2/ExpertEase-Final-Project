namespace ExpertEase.Application.DataTransferObjects.ProtectionFeeDTOs;

// ✅ NEW: Fee calculation details
public record ProtectionFeeDetailsDto
{
    public decimal BaseServiceAmount { get; init; }
    public string FeeType { get; init; } = "percentage";
    public decimal FeePercentage { get; init; }
    public decimal FixedFeeAmount { get; init; }
    public decimal MinimumFee { get; init; }
    public decimal MaximumFee { get; init; }
    public decimal CalculatedFee { get; init; }
    public string FeeJustification { get; init; } = string.Empty;
    public DateTime CalculatedAt { get; init; } = DateTime.UtcNow;
}