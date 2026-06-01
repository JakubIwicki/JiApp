using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.YtDownloader.Features.DownloadHistory;
using JiApp.YtDownloader.Features.SearchHistory;
using JiApp.YtDownloader.Logging;
using JiApp.YtDownloader.Repositories;
using Microsoft.Extensions.Logging;

namespace JiApp.YtDownloader.Features.GetHistory;

public sealed class GetHistoryHandler(
    ISearchHistoryRepository searchHistoryRepository,
    IDownloadHistoryRepository downloadHistoryRepository,
    ICurrentUserService currentUser,
    ILogger<GetHistoryHandler> logger)
{
    public async Task<Result<GetHistoryResponse>> HandleAsync(GetHistoryRequest request)
    {
        var limit = request.Limit ?? 10;

        logger.FetchingCombinedHistory(limit);

        List<YoutubeSearchHistory> searchHistory = [];
        List<YoutubeDownloadHistory> downloadHistory = [];
        bool searchFailed = false, downloadFailed = false;

        try
        {
            var results = await searchHistoryRepository.GetByUserIdAsync(currentUser.UserId, limit);
            searchHistory = results.ToList();
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            logger.FailedToRetrieveSearchHistory(ex);
            searchFailed = true;
        }

        try
        {
            var results = await downloadHistoryRepository.GetByUserIdAsync(currentUser.UserId, limit);
            downloadHistory = results.ToList();
        }
        catch (Exception ex)
        {
            logger.FailedToRetrieveDownloadHistory(ex);
            downloadFailed = true;
        }

        if (searchFailed && downloadFailed)
        {
            logger.BothHistoryRetrievalsFailed();
            return Result<GetHistoryResponse>.Failure("An error occurred while retrieving history");
        }

        var searchItems = searchHistory.Select(SearchHistoryItem.FromEntity).ToList();
        var downloadItems = downloadHistory.Select(DownloadHistoryItem.FromEntity).ToList();

        return Result<GetHistoryResponse>.Success(
            new GetHistoryResponse(searchItems.AsReadOnly(), downloadItems.AsReadOnly()));
    }
}
