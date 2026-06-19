using System.Text.RegularExpressions;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.YtApi;
using JiApp.YtDownloader.Features.DownloadHistory;
using JiApp.YtDownloader.Features.SearchHistory;
using JiApp.YtDownloader.Features.SearchVideos;
using JiApp.YtDownloader.Logging;
using JiApp.YtDownloader.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace JiApp.YtDownloader.Agent;

public sealed partial class YtAgentToolService(
    IYoutubeClient youtubeClient,
    ISearchHistoryRepository searchHistoryRepository,
    IDownloadHistoryRepository downloadHistoryRepository,
    IMemoryCache cache,
    ILogger<YtAgentToolService> logger)
{
    private const int CacheDurationHours = 1;
    private const int MaxYouTubeResults = 50;
    private const int DefaultResultLimit = 10;
    private const string CacheKeyPrefix = "youtube:search";

    [GeneratedRegex(
        @"(?:youtube\.com/(?:watch\?v=|shorts/|embed/)|youtu\.be/)([a-zA-Z0-9_-]{11})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex YouTubeUrlRegex();

    private static string CacheKey(string query) =>
        $"{CacheKeyPrefix}:{query.Trim().ToLowerInvariant()}";

    public async Task<Result<SearchVideosResponse>> SearchAsync(long userId, string query, int? maxResults,
        CancellationToken ct = default)
    {
        try
        {
            var requestedMax = maxResults ?? DefaultResultLimit;
            IReadOnlyList<YoutubeVideo> videos;

            var urlMatch = YouTubeUrlRegex().Match(query);
            if (urlMatch.Success)
            {
                var videoId = urlMatch.Groups[1].Value;
                var video = await youtubeClient.GetVideoByIdAsync(videoId, ct);
                videos = video is not null
                    ? new[] { video }
                    : Array.Empty<YoutubeVideo>();
            }
            else
            {
                var key = CacheKey(query);
                videos = await cache.GetOrCreateAsync(key, async entry =>
                {
                    entry.Size = 1;
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CacheDurationHours);
                    return await youtubeClient.SearchVideosAsync(query, MaxYouTubeResults, ct);
                }) ?? [];
            }

            try
            {
                await searchHistoryRepository.AddAsync(new YoutubeSearchHistory
                {
                    UserId = userId,
                    SearchedAt = DateTime.UtcNow,
                    SearchText = query
                });
                await searchHistoryRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.FailedToSaveSearchHistory(ex, userId);
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
            logger.LogError(ex, "YouTube API search failed for query: {Query}", query);
            return Result<SearchVideosResponse>.Failure(
                "Failed to search videos. Please try again later.");
        }
    }

    public async Task<Result<SearchHistoryResponse>> ListSearchHistoryAsync(long userId, int? limit,
        CancellationToken ct = default)
    {
        var history = await searchHistoryRepository.GetByUserIdAsync(userId, limit ?? DefaultResultLimit);

        var items = history.Select(SearchHistoryItem.FromEntity).ToList();

        return Result<SearchHistoryResponse>.Success(
            new SearchHistoryResponse(items.AsReadOnly()));
    }

    public async Task<Result<DownloadHistoryResponse>> ListDownloadHistoryAsync(long userId, int? limit,
        CancellationToken ct = default)
    {
        var history = await downloadHistoryRepository.GetByUserIdAsync(userId, limit ?? DefaultResultLimit);

        var items = history.Select(DownloadHistoryItem.FromEntity).ToList();

        return Result<DownloadHistoryResponse>.Success(
            new DownloadHistoryResponse(items.AsReadOnly()));
    }

    public DownloadOffer BuildDownloadOffer(long userId, string videoId, string videoUrl, string? title, string? imageUrl) =>
        new(videoId, videoUrl, title, imageUrl, userId);
}
