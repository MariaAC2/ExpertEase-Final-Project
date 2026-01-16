namespace ExpertEase.Application.DataTransferObjects.PhotoDTOs;

public record PortfolioPictureAddDto
{
    public Stream FileStream { get; init; } = null!;
    public string ContentType { get; init; } = null!;
    public string FileName { get; init; } = null!;
}