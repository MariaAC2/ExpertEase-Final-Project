namespace ExpertEase.Application.DataTransferObjects.ServiceTaskDTOs;

public record ServiceTaskAddDto
{
    public Guid UserId { get; init; }
    public Guid SpecialistId { get; init; }
    public Guid PaymentId { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string Description { get; init; } = null!;
    public string Address { get; init; } = null!;
    public decimal Price { get; init; }
}