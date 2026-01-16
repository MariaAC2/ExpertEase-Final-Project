using Google.Cloud.Firestore;

namespace ExpertEase.Domain.Entities;

[FirestoreData]
public class FirestorePhotoDto: FirestoreBaseEntityDto
{
    [FirestoreProperty]
    public string FileName { get; set; } = null!;
    [FirestoreProperty]
    public string Url { get; set; } = null!;
    [FirestoreProperty]
    public string ContentType { get; set; } = null!;
    [FirestoreProperty]
    public long SizeInBytes { get; set; }
    [FirestoreProperty]
    public string UserId { get; set; } = null!;
    [FirestoreProperty]
    public bool IsProfilePicture { get; set; } = false;
}