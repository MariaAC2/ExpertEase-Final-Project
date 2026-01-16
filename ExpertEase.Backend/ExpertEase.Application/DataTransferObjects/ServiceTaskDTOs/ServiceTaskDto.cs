using ExpertEase.Domain.Enums;

namespace ExpertEase.Application.DataTransferObjects.ServiceTaskDTOs;

public record ServiceTaskDto
{
    public Guid Id { get; init; }
    public Guid PaymentId { get; init; }
    public Guid UserId { get; init; }
    public Guid SpecialistId { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string Description { get; init; } = null!;
    public string Address { get; init; } = null!;
    public decimal Price { get; init; }
    public JobStatusEnum Status { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
}