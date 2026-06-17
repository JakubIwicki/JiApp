namespace JiApp.YtDownloader.Agent;

public sealed record DownloadOffer(
    string VideoId,
    string VideoUrl,
    string? Title,
    string? ImageUrl);
