using ExpertEase.Domain.Enums;

namespace ExpertEase.Application.DataTransferObjects;

public record StatusUpdateDto
{
    public Guid Id { get; init; }
    public StatusEnum Status { get; init; }
}

public record JobStatusUpdateDto
{
    public Guid Id { get; init; }
    public JobStatusEnum Status { get; init; }
}