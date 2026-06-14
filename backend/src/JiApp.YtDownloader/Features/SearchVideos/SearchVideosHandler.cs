using System.Text.RegularExpressions;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.YtApi;
using JiApp.YtDownloader.Logging;
using JiApp.YtDownloader.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace JiApp.YtDownloader.Features.SearchVideos;

public sealed partial class SearchVideosHandler(
    IYoutubeClient youtubeClient,
    ISearchHistoryRepository searchHistoryRepository,
    ICurrentUserService currentUser,
    IMemoryCache cache,
    ILogger<SearchVideosHandler> logger)
{
    private const int CacheDurationHours = 1;
    private const int MaxYouTubeResults = 50;
    private const string CacheKeyPrefix = "youtube:search";

    /// <summary>
    /// Matches YouTube URLs and captures the 11-character video ID:
    ///   - https://www.youtube.com/watch?v=VIDEO_ID
    ///   - https://youtu.be/VIDEO_ID
    ///   - https://www.youtube.com/shorts/VIDEO_ID
    ///   - https://www.youtube.com/embed/VIDEO_ID
    ///   - https://m.youtube.com/watch?v=VIDEO_ID
    /// </summary>
    [GeneratedRegex(
        @"(?:youtube\.com/(?:watch\?v=|shorts/|embed/)|youtu\.be/)([a-zA-Z0-9_-]{11})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex YouTubeUrlRegex();

    private static string CacheKey(string query) =>
        $"{CacheKeyPrefix}:{query.Trim().ToLowerInvariant()}";

    public async Task<Result<SearchVideosResponse>> HandleAsync(SearchVideosRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestedMax = request.MaxResults ?? 10;
            IReadOnlyList<YoutubeVideo> videos;

            // Detect YouTube URL → direct video lookup for exact match
            var urlMatch = YouTubeUrlRegex().Match(request.Query);
            if (urlMatch.Success)
            {
                var videoId = urlMatch.Groups[1].Value;
                var video = await youtubeClient.GetVideoByIdAsync(videoId, cancellationToken);
                videos = video is not null
                    ? new[] { video }
                    : Array.Empty<YoutubeVideo>();
            }
            else
            {
                var key = CacheKey(request.Query);
                videos = await cache.GetOrCreateAsync(key, async entry =>
                {
                    entry.Size = 1;
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CacheDurationHours);
                    return await youtubeClient.SearchVideosAsync(
                        request.Query, MaxYouTubeResults, cancellationToken);
                }) ?? [];
            }

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

            var items = videos.Take(requestedMax).Select(v => new VideoItem(
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
