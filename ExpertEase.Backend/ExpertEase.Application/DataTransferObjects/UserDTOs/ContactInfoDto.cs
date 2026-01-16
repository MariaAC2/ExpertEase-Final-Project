namespace ExpertEase.Application.DataTransferObjects.UserDTOs;

public record ContactInfoDto
{
    public string PhoneNumber { get; init; } = null!;
    public string Address { get; init; } = null!;
}
