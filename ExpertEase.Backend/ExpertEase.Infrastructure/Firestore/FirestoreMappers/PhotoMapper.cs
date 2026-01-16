using ExpertEase.Domain.Entities;
using Google.Cloud.Firestore;

namespace ExpertEase.Infrastructure.Firestore.FirestoreMappers;

public static class PhotoMapper
{
    public static FirestorePhotoDto ToFirestoreDto(Photo photo)
    {
        return new FirestorePhotoDto
        {
            Id = photo.Id.ToString(),
            FileName = photo.FileName,
            Url = photo.Url,
            ContentType = photo.ContentType,
            SizeInBytes = photo.SizeInBytes,
            UserId = photo.UserId.ToString(),
            IsProfilePicture = photo.IsProfilePicture,
            CreatedAt = Timestamp.FromDateTime(photo.CreatedAt.ToUniversalTime())
        };
    }

    public static Photo FromFirestoreDto(FirestorePhotoDto dto)
    {
        return new Photo
        {
            Id = Guid.Parse(dto.Id),
            FileName = dto.FileName,
            Url = dto.Url,
            ContentType = dto.ContentType,
            SizeInBytes = dto.SizeInBytes,
            UserId = Guid.Parse(dto.UserId),
            IsProfilePicture = dto.IsProfilePicture,
            CreatedAt = dto.CreatedAt.ToDateTime()
        };
    }
}
