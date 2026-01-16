using ExpertEase.Domain.Enums;

namespace ExpertEase.Application.DataTransferObjects.RequestDTOs;

public record RequestDto
{
    public Guid Id { get; init; }
    public Guid SenderId { get; init; }
    public DateTime RequestedStartDate { get; init; }
    public string Description { get; init; } = null!;
    public string SenderPhoneNumber { get; init; } = null!;
    public string SenderAddress { get; init; } = null!;
    public StatusEnum Status { get; init; } = StatusEnum.Pending;
}