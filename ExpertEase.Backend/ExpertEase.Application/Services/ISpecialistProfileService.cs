using ExpertEase.Application.DataTransferObjects.PhotoDTOs;
using ExpertEase.Application.DataTransferObjects.SpecialistDTOs;
using ExpertEase.Application.DataTransferObjects.SpecialistProfileDTOs;
using ExpertEase.Application.DataTransferObjects.UserDTOs;
using ExpertEase.Application.Requests;
using ExpertEase.Application.Responses;

namespace ExpertEase.Application.Services;

public interface ISpecialistProfileService
{
    Task<ServiceResponse<BecomeSpecialistResponseDto>> AddSpecialistProfile(BecomeSpecialistDto becomeSpecialistProfile, UserDto? requestingUser = null, CancellationToken cancellationToken = default);
    Task<ServiceResponse<SpecialistProfileDto>> GetSpecialistProfile(Guid userId, CancellationToken cancellationToken = default);

    Task<ServiceResponse> UpdateSpecialistProfile(
        SpecialistProfileUpdateDto specialistProfile,
        List<PortfolioPictureAddDto>? newPhotos = null,
        UserDto? requestingUser = null,
        CancellationToken cancellationToken = default);
    Task<ServiceResponse> DeleteSpecialistProfile(Guid id, UserDto? requestingUser = null, CancellationToken cancellationToken = default);
}
