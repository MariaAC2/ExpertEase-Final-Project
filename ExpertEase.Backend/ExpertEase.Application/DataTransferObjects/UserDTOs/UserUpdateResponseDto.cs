namespace ExpertEase.Application.DataTransferObjects.UserDTOs;

public record UserUpdateResponseDto
{
    public string? Token { get; init; }
    public UserUpdateDto? User { get; init; }
}
