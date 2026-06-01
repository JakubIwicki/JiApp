using JiApp.Common.Abstractions;
using JiApp.YtDownloader.Logging;
using JiApp.YtDownloader.Repositories;
using Microsoft.Extensions.Logging;

namespace JiApp.YtDownloader.Features.SearchHistory;

public sealed class SearchHistoryHandler(
    ISearchHistoryRepository searchHistoryRepository,
    ICurrentUserService currentUser,
    ILogger<SearchHistoryHandler> logger)
{
    public async Task<Result<SearchHistoryResponse>> HandleAsync(SearchHistoryRequest request)
    {
        var limit = request.Limit ?? 10;

        logger.FetchingSearchHistory(limit);

        var history = await searchHistoryRepository.GetByUserIdAsync(currentUser.UserId, limit);

        var items = history.Select(SearchHistoryItem.FromEntity).ToList();

        return Result<SearchHistoryResponse>.Success(
            new SearchHistoryResponse(items.AsReadOnly()));
    }
}
