namespace ExpertEase.Application.DataTransferObjects.ReplyDTOs;

public record ReplyAddDto
{
    public DateTime? StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal Price { get; init; }
}