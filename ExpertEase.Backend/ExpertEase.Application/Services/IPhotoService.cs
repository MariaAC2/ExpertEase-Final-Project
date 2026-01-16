using ExpertEase.Application.DataTransferObjects.FirestoreDTOs;
using ExpertEase.Application.DataTransferObjects.PhotoDTOs;
using ExpertEase.Application.DataTransferObjects.UserDTOs;
using ExpertEase.Application.Responses;

namespace ExpertEase.Application.Services;

public interface IPhotoService
{
    private Task<string> AddPhoto(PhotoAddDto photo, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
    
    private Task DeletePhoto<TServiceResponse>(string id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
    
    Task<ServiceResponse> AddProfilePicture(ProfilePictureAddDto photo, UserDto? requestingUser = null,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse> AddPortfolioPicture(PortfolioPictureAddDto photo, UserDto? requestingUser = null,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse> UpdateProfilePicture(ProfilePictureAddDto photoDto, UserDto? requestingUser = null,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse> DeletePortfolioPicture(string photoId, UserDto? requestingUser = null,
        CancellationToken cancellationToken = default);

    Task<ServiceResponse> AddPhotoToConversation(
        Guid receiverId,
        ConversationPhotoUploadDto photoUpload,
        UserDto? sender,
        CancellationToken cancellationToken = default);
}