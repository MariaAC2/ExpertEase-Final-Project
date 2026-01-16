namespace ExpertEase.Application.DataTransferObjects.PhotoDTOs;

public record ProfilePictureAddDto
{
    public Stream FileStream { get; init; } = null!;
    public string ContentType { get; init; } = null!;
}