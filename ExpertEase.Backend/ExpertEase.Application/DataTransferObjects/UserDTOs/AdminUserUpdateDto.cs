using ExpertEase.Domain.Enums;

namespace ExpertEase.Application.DataTransferObjects.UserDTOs;

public record AdminUserUpdateDto
{
    public Guid Id { get; init; }
    public string? FullName { get; init; }
    public UserRoleEnum? Role { get; init; }
}
