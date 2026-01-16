using ExpertEase.Domain.Enums;

namespace ExpertEase.Application.DataTransferObjects.ReplyDTOs;

public record ReplyDto
{
    public Guid Id { get; init; }
    public Guid SenderId { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal Price { get; init; }
    public StatusEnum Status { get; init; }
}