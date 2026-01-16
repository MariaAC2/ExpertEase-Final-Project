using System.Net;
using ExpertEase.Application.DataTransferObjects.FirestoreDTOs;
using ExpertEase.Application.DataTransferObjects.PhotoDTOs;
using ExpertEase.Application.DataTransferObjects.UserDTOs;
using ExpertEase.Application.Errors;
using ExpertEase.Application.Responses;
using ExpertEase.Application.Services;
using ExpertEase.Domain.Entities;
using ExpertEase.Domain.Enums;
using ExpertEase.Domain.Specifications;
using ExpertEase.Infrastructure.Database;
using ExpertEase.Infrastructure.Firestore.FirestoreDTOs;
using ExpertEase.Infrastructure.Firestore.FirestoreMappers;
using ExpertEase.Infrastructure.Firestore.FirestoreRepository;
using ExpertEase.Infrastructure.Repositories;

namespace ExpertEase.Infrastructure.Services;

public class PhotoService(IRepository<WebAppDatabaseContext> repository, 
    IFirestoreRepository firestoreRepository, 
    IFirebaseStorageService firebaseStorageService,
    IConversationService conversationService): IPhotoService
{
    // Maximum file size for conversation photos (e.g., 10MB)
    private const long MaxConversationPhotoSize = 10 * 1024 * 1024;
    
    // Allowed content types for conversation photos
    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp"
    ];
    private async Task<string> AddPhoto(PhotoAddDto photo, CancellationToken cancellationToken = default)
    {
        var url = await firebaseStorageService.UploadImageAsync(photo.FileStream, photo.Folder, photo.FileName, photo.ContentType);

        long sizeInBytes = 0;
        if (photo.FileStream.CanSeek)
        {
            sizeInBytes = photo.FileStream.Length;
        }

        var domainPhoto = new Photo
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Parse(photo.UserId),
            Url = url,
            ContentType = photo.ContentType,
            FileName = photo.FileName,
            SizeInBytes = sizeInBytes,
            IsProfilePicture = photo.IsProfilePicture,
            CreatedAt = DateTime.UtcNow
        };

        var firestoreDto = PhotoMapper.ToFirestoreDto(domainPhoto);
        await firestoreRepository.AddAsync("photos", firestoreDto, cancellationToken);
        return firestoreDto.Url;
    }

    private async Task<ServiceResponse> DeletePhoto(string id, CancellationToken cancellationToken = default)
    {
        var photo = await firestoreRepository.GetAsync<FirestorePhotoDto>("photos", id, cancellationToken);
        if (photo == null)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.NotFound, "Photo not found"));
        }

        var objectName = new Uri(photo.Url).AbsolutePath.TrimStart('/');

        await firebaseStorageService.DeleteImageAsync(objectName, cancellationToken);
        await firestoreRepository.DeleteAsync<FirestorePhotoDto>("photos", id, cancellationToken);

        return ServiceResponse.CreateSuccessResponse();
    }

    public async Task<ServiceResponse> AddProfilePicture(ProfilePictureAddDto photo, UserDto? requestingUser = null, CancellationToken cancellationToken = default)
    {
        if (requestingUser == null)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden, "User not found", ErrorCodes.CannotAdd));
        }

        var photoAddDto = new PhotoAddDto
        {
            UserId = requestingUser.Id.ToString(),
            FileStream = photo.FileStream,
            Folder = "profile_pictures",
            FileName = requestingUser.Id.ToString(),
            ContentType = photo.ContentType,
            IsProfilePicture = true
        };

        var photoUrl = await AddPhoto(photoAddDto, cancellationToken);

        var user = await repository.GetAsync(new UserSpec(requestingUser.Id), cancellationToken);
        if (user == null)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.NotFound, "User not found", ErrorCodes.EntityNotFound));
        }

        user.ProfilePictureUrl = photoUrl;
        await repository.UpdateAsync(user, cancellationToken);
        return ServiceResponse.CreateSuccessResponse();
    }

    public async Task<ServiceResponse> AddPortfolioPicture(PortfolioPictureAddDto photo, UserDto? requestingUser = null, CancellationToken cancellationToken = default)
    {
        if (requestingUser == null)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden, "User not found", ErrorCodes.CannotAdd));
        }

        var photoAddDto = new PhotoAddDto
        {
            UserId = requestingUser.Id.ToString(),
            FileStream = photo.FileStream,
            Folder = "portfolio_pictures/" + requestingUser.Id,
            FileName = photo.FileName,
            ContentType = photo.ContentType,
            IsProfilePicture = false
        };

        var photoUrl = await AddPhoto(photoAddDto, cancellationToken);

        var user = await repository.GetAsync(new UserSpec(requestingUser.Id), cancellationToken);
        if (user == null)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.NotFound, "User not found", ErrorCodes.EntityNotFound));
        }

        if (user.Role != UserRoleEnum.Specialist || user.SpecialistProfile == null)
        {
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden, "Only specialists can have portfolio pictures", ErrorCodes.CannotAdd));
        }

        // user.SpecialistProfile.Portfolio.Add(photoUrl);
        // await repository.UpdateAsync(user, cancellationToken);
        return ServiceResponse.CreateSuccessResponse();
    }

    public async Task<ServiceResponse> UpdateProfilePicture(ProfilePictureAddDto photo, UserDto? requestingUser = null, CancellationToken cancellationToken = default)
    {
        if (requestingUser == null)
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.Forbidden, "User not found", ErrorCodes.CannotAdd));

        var existingPhotos = await firestoreRepository.ListAsync<FirestorePhotoDto>(
            "photos",
            col => col.WhereEqualTo("UserId", requestingUser.Id.ToString()).WhereEqualTo("IsProfilePicture", true),
            cancellationToken
        );

        if (existingPhotos.Any())
        {
            var oldPhoto = existingPhotos.First();
            await DeletePhoto(oldPhoto.Id, cancellationToken);
        }

        var photoDto = new PhotoAddDto
        {
            UserId = requestingUser.Id.ToString(),
            FileStream = photo.FileStream,
            Folder = "profile_pictures",
            FileName = requestingUser.Id.ToString(),
            ContentType = photo.ContentType,
            IsProfilePicture = true
        };

        var newUrl = await AddPhoto(photoDto, cancellationToken);

        var user = await repository.GetAsync(new UserSpec(requestingUser.Id), cancellationToken);
        if (user == null)
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.NotFound, "User not found"));

        user.ProfilePictureUrl = newUrl;
        await repository.UpdateAsync(user, cancellationToken);

        return ServiceResponse.CreateSuccessResponse(new { profileImageUrl = newUrl });
    }

    public async Task<ServiceResponse> DeletePortfolioPicture(string photoId, UserDto? requestingUser = null, CancellationToken cancellationToken = default)
    {
        var photo = await firestoreRepository.GetAsync<FirestorePhotoDto>("photos", photoId, cancellationToken);
        if (photo == null || photo.UserId != requestingUser.Id.ToString())
            return ServiceResponse.CreateErrorResponse(new(HttpStatusCode.NotFound, "Photo not found or unauthorized"));

        var objectName = new Uri(photo.Url).AbsolutePath.TrimStart('/');
        var user = await repository.GetAsync(new UserSpec(requestingUser.Id), cancellationToken);
        if (user?.SpecialistProfile?.Portfolio == null) return ServiceResponse.CreateErrorResponse(new ErrorMessage(HttpStatusCode.Conflict, "User can't remove a non existent photo", ErrorCodes.CannotDelete));
        await firebaseStorageService.DeleteImageAsync(objectName, cancellationToken);

        await firestoreRepository.DeleteAsync<FirestorePhotoDto>("photos", photoId, cancellationToken);
        
        user.SpecialistProfile.Portfolio.Remove(photo.Url);
        await repository.UpdateAsync(user, cancellationToken);

        return ServiceResponse.CreateSuccessResponse();
    }

    private static async Task<ServiceResponse> ValidateConversationPhoto(
        Stream fileStream, 
        string contentType, 
        long fileSize)
    {
        // Check file size
        if (fileSize > MaxConversationPhotoSize)
        {
            return ServiceResponse.CreateErrorResponse(
                new ErrorMessage(HttpStatusCode.BadRequest, 
                    $"File size exceeds maximum allowed size of {MaxConversationPhotoSize / (1024 * 1024)}MB"));
        }

        // Check content type
        if (!AllowedContentTypes.Contains(contentType.ToLower()))
        {
            return ServiceResponse.CreateErrorResponse(
                new ErrorMessage(HttpStatusCode.BadRequest, 
                    $"Unsupported file type. Allowed types: {string.Join(", ", AllowedContentTypes)}"));
        }

        // Additional validation can be added here (e.g., image dimensions, file content validation)
        
        return ServiceResponse.CreateSuccessResponse();
    }
    
    public async Task<ServiceResponse> AddPhotoToConversation(
        Guid receiverId,
        ConversationPhotoUploadDto photoUpload,
        UserDto? sender,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Validate photo
            var validationResult = await ValidateConversationPhoto(
                photoUpload.FileStream, 
                photoUpload.ContentType, 
                photoUpload.FileStream.Length);
            if (!validationResult.IsSuccess)
                return validationResult;

            var photoDto = new PhotoAddDto
            {
                ContentType = photoUpload.ContentType,
                Folder = "conversations",
                FileName = photoUpload.FileName,
                FileStream = photoUpload.FileStream,
                IsProfilePicture = false,
                UserId = sender?.Id.ToString() ?? string.Empty
            };

            if (photoDto.FileStream == null)
            {
                return ServiceResponse.CreateErrorResponse(new ErrorMessage(HttpStatusCode.BadRequest, "File stream is null", ErrorCodes.Invalid));
            }

            // 2. Generate unique filename and upload to storage
            var newUrl = await AddPhoto(photoDto, cancellationToken);

            // Set the URL in the photo data (we'll handle this in AddPhotoMessage)
            var photoData = new Dictionary<string, object>
            {
                ["fileName"] = photoUpload.FileName,
                ["url"] = newUrl,
                ["contentType"] = photoUpload.ContentType,
                ["sizeInBytes"] = photoUpload.FileStream.Length,
                ["uploadedAt"] = DateTime.UtcNow,
            };

            var conversationItemAdd = new FirestoreConversationItemAddDto
            {
                Type = "photo",
                Data = photoData
            };

            // 4. Use the conversation service to add the item directly
            var addResult = await conversationService.AddConversationItem(
                conversationItemAdd,
                receiverId,
                sender,
                cancellationToken);

            if (!addResult.IsSuccess)
            {
                // If adding to conversation failed, clean up the uploaded image
                try
                {
                    var objectName = new Uri(newUrl).AbsolutePath.TrimStart('/');
                    await firebaseStorageService.DeleteImageAsync(objectName, cancellationToken);
                }
                catch (Exception cleanupEx)
                {
                    // Log cleanup failure but don't throw
                    _ = cleanupEx; // referenced to avoid unused variable warning
                }
                
                return addResult;
            }

            return ServiceResponse.CreateSuccessResponse();
        }
        catch (Exception ex)
        {
            return ServiceResponse.CreateErrorResponse(
                new ErrorMessage(HttpStatusCode.InternalServerError, $"Failed to add photo: {ex.Message}"));
        }
    }
}