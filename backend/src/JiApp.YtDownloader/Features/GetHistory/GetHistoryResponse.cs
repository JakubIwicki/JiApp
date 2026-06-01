using JiApp.YtDownloader.Features.DownloadHistory;
using JiApp.YtDownloader.Features.SearchHistory;

namespace JiApp.YtDownloader.Features.GetHistory;

[Serializable]
public sealed record GetHistoryResponse(
    IReadOnlyList<SearchHistoryItem> Searches,
    IReadOnlyList<DownloadHistoryItem> Downloads
);