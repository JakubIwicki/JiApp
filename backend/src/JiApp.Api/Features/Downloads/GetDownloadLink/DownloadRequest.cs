namespace JiApp.Api.Features.Downloads.GetDownloadLink;

public sealed record DownloadRequest(
    string VideoId,
    string VideoUrl,
    string? Title,
    string? Description,
    string? ImageUrl);
