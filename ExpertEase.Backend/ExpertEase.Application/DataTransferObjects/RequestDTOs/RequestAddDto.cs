
namespace ExpertEase.Application.DataTransferObjects.RequestDTOs;
public record RequestAddDto
{
    public Guid ReceiverUserId { get; init; }
    public DateTime RequestedStartDate { get; init; }
    public string PhoneNumber { get; init; } = null!;
    public string Address { get; init; } = null!;
    public string Description { get; init; } = null!;
}