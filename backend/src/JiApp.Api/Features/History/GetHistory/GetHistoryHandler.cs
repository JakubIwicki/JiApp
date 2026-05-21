using JiApp.Api.Features.Downloads.DownloadHistory;
using JiApp.Api.Features.Search.SearchHistory;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Infrastructure.Repositories;

namespace JiApp.Api.Features.History.GetHistory;

public sealed class GetHistoryHandler(
    ISearchHistoryRepository searchHistoryRepository,
    IDownloadHistoryRepository downloadHistoryRepository,
    ICurrentUserService currentUser)
{
    public async Task<Result<GetHistoryResponse>> HandleAsync(GetHistoryRequest request)
    {
        var limit = request.Limit ?? 10;

        List<YoutubeSearchHistory> searchHistory = [];
        List<YoutubeDownloadHistory> downloadHistory = [];
        bool searchFailed = false, downloadFailed = false;

        try
        {
            var results = await searchHistoryRepository.GetByUserIdAsync(currentUser.UserId, limit);
            searchHistory = results.ToList();
        }
        catch
        {
            searchFailed = true;
        }

        try
        {
            var results = await downloadHistoryRepository.GetByUserIdAsync(currentUser.UserId, limit);
            downloadHistory = results.ToList();
        }
        catch
        {
            downloadFailed = true;
        }

        if (searchFailed && downloadFailed)
        {
            return Result<GetHistoryResponse>.Failure("An error occurred while retrieving history");
        }

        var searchItems = searchHistory.Select(h => new SearchHistoryItem(
            h.Id,
            h.SearchText ?? string.Empty,
            h.SearchedAt ?? DateTime.MinValue
        )).ToList();

        var downloadItems = downloadHistory.Select(h => new DownloadHistoryItem(
            h.Id,
            h.VideoTitle,
            h.VideoDescription,
            h.VideoId ?? string.Empty,
            h.VideoUrl ?? string.Empty,
            h.ImageUrl,
            h.DownloadedAt
        )).ToList();

        return Result<GetHistoryResponse>.Success(
            new GetHistoryResponse(searchItems.AsReadOnly(), downloadItems.AsReadOnly()));
    }
}
