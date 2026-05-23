using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JiApp.Api.Features.Downloads.DownloadHistory;
using JiApp.Api.Features.Search.SearchHistory;
using JiApp.Api.Logging;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace JiApp.Api.Features.History.GetHistory;

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
#pragma warning disable CA1031 // Reading from independent repositories; one failure should not prevent the other
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