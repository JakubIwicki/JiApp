using JiApp.Common.Abstractions;
using JiApp.Infrastructure.Repositories;

namespace JiApp.Api.Features.Search.SearchHistory;

public sealed class SearchHistoryHandler(
    ISearchHistoryRepository searchHistoryRepository,
    ICurrentUserService currentUser)
{
    public async Task<Result<SearchHistoryResponse>> HandleAsync(SearchHistoryRequest request)
    {
        var limit = request.Limit ?? 10;

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
