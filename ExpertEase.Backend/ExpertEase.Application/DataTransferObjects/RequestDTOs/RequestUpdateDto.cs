using System.ComponentModel.DataAnnotations;
using ExpertEase.Domain.Enums;

namespace ExpertEase.Application.DataTransferObjects.RequestDTOs;

public record RequestUpdateDto
{
    public Guid Id { get; init; }
    public DateTime? RequestedStartDate { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Address { get; init; }
    public string? Description { get; init; }
}