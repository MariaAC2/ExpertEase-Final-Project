namespace ExpertEase.Application.DataTransferObjects.FirestoreDTOs;

public class ConversationPhotoUploadDto
{
    public Stream FileStream { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
}