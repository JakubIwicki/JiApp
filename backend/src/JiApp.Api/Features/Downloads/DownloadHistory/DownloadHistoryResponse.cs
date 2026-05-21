namespace JiApp.Api.Features.Downloads.DownloadHistory;

public sealed record DownloadHistoryResponse(IReadOnlyList<DownloadHistoryItem> Items);

public sealed record DownloadHistoryItem(
    long Id,
    string? VideoTitle,
    string? VideoDescription,
    string VideoId,
    string VideoUrl,
    string? ImageUrl,
    DateTime DownloadedAt);
