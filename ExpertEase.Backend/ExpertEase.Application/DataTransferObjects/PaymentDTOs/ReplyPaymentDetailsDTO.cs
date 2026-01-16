namespace ExpertEase.Application.DataTransferObjects.PaymentDTOs;

public class ReplyPaymentDetailsDto
{
    public string ReplyId { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public Guid ClientId { get; set; }
    public Guid SpecialistId { get; set; }
}