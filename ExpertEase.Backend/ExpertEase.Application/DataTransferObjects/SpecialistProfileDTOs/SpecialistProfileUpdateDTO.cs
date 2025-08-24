using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ExpertEase.Application.DataTransferObjects.SpecialistDTOs;

public class SpecialistProfileUpdateDTO
{
    [Required]
    public Guid UserId { get; set; }
    public string? PhoneNumber { get; set; } = null!;
    public string? Address { get; set; } = null!;
    public int? YearsExperience { get; set; }
    public string? Description { get; set; } = null!;
    public List<Guid>? CategoryIds { get; set; }
    public List<string>? ExistingPortfolioPhotoUrls { get; set; }
    public List<string>? PhotoIdsToRemove { get; set; }
}

public class SpecialistProfileUpdateFormDTO
{
    [Required]
    public Guid UserId { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public int? YearsExperience { get; set; }
    public string? Description { get; set; }
    public Guid[]? CategoryIds { get; set; }
    public IFormFile[]? NewPortfolioPhotos { get; set; }
    public string[]? ExistingPortfolioPhotoUrls { get; set; }
    public string[]? PhotoIdsToRemove { get; set; }
}