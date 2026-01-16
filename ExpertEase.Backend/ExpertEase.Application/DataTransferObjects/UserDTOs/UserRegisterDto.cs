namespace ExpertEase.Application.DataTransferObjects.UserDTOs;

public record UserRegisterDto
{
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string Password { get; init; } = null!;
}