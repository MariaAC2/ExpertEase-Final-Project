
using ExpertEase.Infrastructure.Firestore.FirestoreDTOs;

namespace ExpertEase.Application.DataTransferObjects.FirestoreDTOs;

public class ConversationDto
{
    public Guid ConversationId { get; set; }
    public Guid? UserId { get; set; }
    public string? UserFullName { get; set; } = string.Empty;
    public string? UserProfilePictureUrl { get; set; } = string.Empty;
    public List<FirestoreConversationItemDto> ConversationItems { get; set; } = new();
}

public class UserConversationDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public string? UserProfilePictureUrl { get; set; } = string.Empty;
    public string? LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; } = DateTime.MinValue;
    public int UnreadCount { get; set; } = 0;
}

public class ConversationItemDto
{
    public Guid Id { get; set; }
    public string ConversationId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "message", "request", "reply"
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}