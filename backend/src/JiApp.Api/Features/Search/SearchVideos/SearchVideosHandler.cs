using System;
using System.Linq;
using System.Threading.Tasks;
using Google;
using JiApp.Api.Logging;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Infrastructure.Repositories;
using JiApp.YtApi;
using Microsoft.Extensions.Logging;

namespace JiApp.Api.Features.Search.SearchVideos;

public sealed class SearchVideosHandler(
    IYoutubeClient youtubeClient,
    ISearchHistoryRepository searchHistoryRepository,
    ICurrentUserService currentUser,
    ILogger<SearchVideosHandler> logger)
{
    public async Task<Result<SearchVideosResponse>> HandleAsync(SearchVideosRequest request)
    {
        try
        {
            var maxResults = request.MaxResults ?? 10;

            var videos = await youtubeClient.SearchVideosAsync(request.Query, maxResults);

            var historyEntry = new YoutubeSearchHistory
            {
                UserId = currentUser.UserId,
                SearchedAt = DateTime.UtcNow,
                SearchText = request.Query
            };

            try
            {
                await searchHistoryRepository.AddAsync(historyEntry);
                await searchHistoryRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.FailedToSaveSearchHistory(ex, currentUser.UserId);
            }

            var items = videos.Select(v => new VideoItem(
                v.VideoId,
                v.Title,
                v.Description,
                v.ImageUrl,
                v.VideoUrl,
                v.ChannelTitle
            )).ToList();

            return Result<SearchVideosResponse>.Success(
                new SearchVideosResponse(items.AsReadOnly()));
        }
        catch (GoogleApiException ex)
        {
            return Result<SearchVideosResponse>.Failure(
                $"Failed to search videos: {ex.Message}");
        }
    }
}