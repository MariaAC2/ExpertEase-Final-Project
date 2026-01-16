namespace ExpertEase.Application.DataTransferObjects.ServiceTaskDTOs;

public record ServiceTaskDetailsDto
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string SpecialistName { get; init; } = string.Empty;
}