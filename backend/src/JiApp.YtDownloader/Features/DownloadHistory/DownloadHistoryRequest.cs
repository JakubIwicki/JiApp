namespace JiApp.YtDownloader.Features.DownloadHistory;

[Serializable]
public sealed record DownloadHistoryRequest(int? Limit);