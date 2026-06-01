using JiApp.Common.Models;

namespace JiApp.YtDownloader.Features.DownloadHistory;

[Serializable]
public sealed record DownloadHistoryResponse(IReadOnlyList<DownloadHistoryItem> Items);

[Serializable]
public sealed record DownloadHistoryItem(
    long Id,
    string? VideoTitle,
    string? VideoDescription,
    string VideoId,
    string VideoUrl,
    string? ImageUrl,
    DateTime DownloadedAt)
{
    public static DownloadHistoryItem FromEntity(YoutubeDownloadHistory entity) =>
        new(entity.Id, entity.VideoTitle, entity.VideoDescription,
            entity.VideoId ?? string.Empty, entity.VideoUrl ?? string.Empty,
            entity.ImageUrl, entity.DownloadedAt);
}