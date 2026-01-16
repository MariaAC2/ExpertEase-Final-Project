
namespace ExpertEase.Application.DataTransferObjects.ServiceTaskDTOs;

public record ServiceTaskUpdateDto
{
    public Guid Id { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? Address { get; init; }
    public decimal? Price { get; init; }
}