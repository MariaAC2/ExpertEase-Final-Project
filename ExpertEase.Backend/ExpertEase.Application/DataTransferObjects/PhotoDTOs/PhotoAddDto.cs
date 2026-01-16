namespace ExpertEase.Application.DataTransferObjects.PhotoDTOs;

public record PhotoAddDto
{
    public Stream? FileStream { get; init; }
    public string Folder { get; init; } = null!;
    public string FileName { get; init; } = null!;
    public string ContentType { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public bool IsProfilePicture { get; init; } = false;
}
