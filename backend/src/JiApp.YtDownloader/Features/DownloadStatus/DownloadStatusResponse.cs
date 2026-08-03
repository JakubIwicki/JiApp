namespace JiApp.YtDownloader.Features.DownloadStatus;

[Serializable]
public sealed record DownloadStatusResponse(string Status, string? Error, string? ErrorCategory);
