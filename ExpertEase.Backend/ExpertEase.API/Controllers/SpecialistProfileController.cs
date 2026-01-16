using ExpertEase.Application.DataTransferObjects.PhotoDTOs;
using ExpertEase.Application.DataTransferObjects.SpecialistDTOs;
using ExpertEase.Application.DataTransferObjects.SpecialistProfileDTOs;
using ExpertEase.Application.Responses;
using ExpertEase.Application.Services;
using ExpertEase.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpertEase.API.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class SpecialistProfileController(IUserService userService, ISpecialistProfileService specialistService) : AuthorizedController(userService)
{
    [Authorize(Roles = "Client")]
    [HttpPut]
    public async Task<ActionResult<RequestResponse<BecomeSpecialistResponseDto>>> BecomeSpecialist([FromBody] BecomeSpecialistFormDto becomeSpecialistForm)
    {
        var currentUser = await GetCurrentUser();
        
        var becomeSpecialistProfile = new BecomeSpecialistDto
        {
            UserId = becomeSpecialistForm.UserId,
            PhoneNumber = becomeSpecialistForm.PhoneNumber,
            Address = becomeSpecialistForm.Address,
            YearsExperience = becomeSpecialistForm.YearsExperience,
            Description = becomeSpecialistForm.Description,
            Categories = becomeSpecialistForm.Categories,
            PortfolioPhotos = becomeSpecialistForm.PortfolioPhotos?.Count > 0 
                ? becomeSpecialistForm.PortfolioPhotos.Select(file => new PortfolioPictureAddDto
                {
                    FileStream = file.OpenReadStream(),
                    ContentType = file.ContentType,
                    FileName = file.FileName
                }).ToList()
                : []
        };

        return currentUser.Result != null ?
            CreateRequestResponseFromServiceResponse(await specialistService.AddSpecialistProfile(becomeSpecialistProfile, currentUser.Result)) :
            CreateErrorMessageResult<BecomeSpecialistResponseDto>(currentUser.Error);
    }
    
    [Authorize(Roles = "Specialist")]
    [HttpGet]
    public async Task<ActionResult<RequestResponse<SpecialistProfileDto>>> Get()
    {
        var currentUser = await GetCurrentUser();

        return currentUser.Result != null ?
            CreateRequestResponseFromServiceResponse(await specialistService.GetSpecialistProfile(currentUser.Result.Id)) :
            CreateErrorMessageResult<SpecialistProfileDto>(currentUser.Error);
    }
    
    [Authorize(Roles = "Specialist")]
    [HttpPatch]
    public async Task<ActionResult<RequestResponse>> Update([FromForm] SpecialistProfileUpdateFormDto updateForm)
    {
        var currentUser = await GetCurrentUser();

        if (currentUser.Result == null)
            return CreateErrorMessageResult(currentUser.Error);

        // Convert form data to service DTO
        var updateDto = new SpecialistProfileUpdateDto
        {
            UserId = updateForm.UserId,
            PhoneNumber = updateForm.PhoneNumber,
            Address = updateForm.Address,
            YearsExperience = updateForm.YearsExperience,
            Description = updateForm.Description,
            CategoryIds = updateForm.CategoryIds?.ToList(), // ADD THIS LINE
            
            ExistingPortfolioPhotoUrls = updateForm.ExistingPortfolioPhotoUrls?.ToList(),
            PhotoIdsToRemove = updateForm.PhotoIdsToRemove?.ToList()
        };

        // Convert new photos to DTOs
        var newPhotos = updateForm.NewPortfolioPhotos?.Select(file => new PortfolioPictureAddDto
        {
            FileStream = file.OpenReadStream(),
            ContentType = file.ContentType,
            FileName = file.FileName
        }).ToList();

        return CreateRequestResponseFromServiceResponse(
            await specialistService.UpdateSpecialistProfile(updateDto, newPhotos, currentUser.Result));
    }
}