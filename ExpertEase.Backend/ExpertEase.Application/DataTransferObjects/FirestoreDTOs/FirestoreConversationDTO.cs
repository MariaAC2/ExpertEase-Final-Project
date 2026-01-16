using ExpertEase.Domain.Entities;
using Google.Cloud.Firestore;

namespace ExpertEase.Application.DataTransferObjects.FirestoreDTOs;

[FirestoreData]
public class FirestoreConversationDto: FirestoreBaseEntityDto
{
    [FirestoreProperty]
    public List<string> ParticipantIds { get; set; } = new List<string>();
    [FirestoreProperty]
    public string Participants { get; set; } = string.Empty;
    [FirestoreProperty]
    public string? LastMessage { get; set; } = string.Empty;
    [FirestoreProperty]
    public Timestamp LastMessageAt { get; set; }
    [FirestoreProperty]
    public Dictionary<string, int> UnreadCounts { get; set; } = new Dictionary<string, int>();
    [FirestoreProperty]
    public FirestoreUserConversationDto ClientData { get; set; } = new FirestoreUserConversationDto();
    [FirestoreProperty]
    public FirestoreUserConversationDto SpecialistData { get; set; } = new FirestoreUserConversationDto();
    [FirestoreProperty] 
    public string RequestId { get; set; } = string.Empty;
}

[FirestoreData]
public class FirestoreUserConversationDto
{
    [FirestoreProperty]
    public string UserId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string UserFullName { get; set; } = string.Empty;

    [FirestoreProperty]
    public string? UserProfilePictureUrl { get; set; }
}