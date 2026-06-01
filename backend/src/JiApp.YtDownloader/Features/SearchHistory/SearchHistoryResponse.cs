using JiApp.Common.Models;

namespace JiApp.YtDownloader.Features.SearchHistory;

[Serializable]
public sealed record SearchHistoryResponse(IReadOnlyList<SearchHistoryItem> Items);

[Serializable]
public sealed record SearchHistoryItem(long Id, string SearchText, DateTime SearchedAt)
{
    public static SearchHistoryItem FromEntity(YoutubeSearchHistory entity) =>
        new(entity.Id, entity.SearchText ?? string.Empty, entity.SearchedAt ?? DateTime.MinValue);
}