using Google.Cloud.Firestore;

namespace ExpertEase.Domain.Entities;

[FirestoreData]
public abstract class FirestoreBaseEntityDto
{
    [FirestoreDocumentId] 
    public string Id { get; set; } = null!;

    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; }
}