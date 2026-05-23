using System;
using System.Linq;
using System.Threading.Tasks;
using JiApp.Api.Logging;
using JiApp.Common.Abstractions;
using JiApp.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace JiApp.Api.Features.Search.SearchHistory;

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

        var items = history.Select(h => new SearchHistoryItem(
            h.Id,
            h.SearchText ?? string.Empty,
            h.SearchedAt ?? DateTime.MinValue
        )).ToList();

        return Result<SearchHistoryResponse>.Success(
            new SearchHistoryResponse(items.AsReadOnly()));
    }
}
