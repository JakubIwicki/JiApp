using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.YtApi;
using JiApp.YtDownloader.Logging;
using JiApp.YtDownloader.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace JiApp.YtDownloader.Features.SearchVideos;

public sealed class SearchVideosHandler(
    IYoutubeClient youtubeClient,
    ISearchHistoryRepository searchHistoryRepository,
    ICurrentUserService currentUser,
    IMemoryCache cache,
    ILogger<SearchVideosHandler> logger)
{
    private const int CacheDurationHours = 1;
    private const int MaxYouTubeResults = 50;
    private const string CacheKeyPrefix = "youtube:search";

    private static string CacheKey(string query) =>
        $"{CacheKeyPrefix}:{query.Trim().ToLowerInvariant()}";

    public async Task<Result<SearchVideosResponse>> HandleAsync(SearchVideosRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var key = CacheKey(request.Query);
            var requestedMax = request.MaxResults ?? 10;

            var videos = await cache.GetOrCreateAsync(key, async entry =>
            {
                entry.Size = 1;
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CacheDurationHours);
                return await youtubeClient.SearchVideosAsync(request.Query, MaxYouTubeResults, cancellationToken);
            });

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

            var items = videos!.Take(requestedMax).Select(v => new VideoItem(
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
        catch (Google.GoogleApiException ex)
        {
            logger.LogError(ex, "YouTube API search failed for query: {Query}", request.Query);
            return Result<SearchVideosResponse>.Failure(
                "Failed to search videos. Please try again later.");
        }
    }
}
