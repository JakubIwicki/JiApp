namespace JiApp.YtDownloader.Features.GetDownloadLink;

[Serializable]
public sealed record DownloadRequest(
    string VideoId,
    string VideoUrl,
    string? Title,
    string? Description,
    string? ImageUrl);
