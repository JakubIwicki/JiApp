using JiApp.Api.Features.Downloads.DownloadHistory;
using JiApp.Api.Features.Search.SearchHistory;

namespace JiApp.Api.Features.History.GetHistory;

public sealed record GetHistoryResponse(
    IReadOnlyList<SearchHistoryItem> Searches,
    IReadOnlyList<DownloadHistoryItem> Downloads
);
