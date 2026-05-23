using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google;
using JiApp.Api.Logging;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Infrastructure.Repositories;
using JiApp.YtApi;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace JiApp.Api.Features.Search.SearchVideos;

public sealed class SearchVideosHandler(
    IYoutubeClient youtubeClient,
    ISearchHistoryRepository searchHistoryRepository,
    ICurrentUserService currentUser,
    IMemoryCache cache,
    ILogger<SearchVideosHandler> logger)
{
    private const int CacheDurationHours = 1;
    private const int MaxYouTubeResults = 50;

    /// <summary>
    /// Builds a normalized, global cache key for YouTube search results.
    ///
    /// Decisions:
    /// - Global scope (no userId): YouTube search results are the same regardless of who asks.
    ///   Sharing across users maximizes cache hits and quota savings.
    /// - Normalized query (trim + lowercase): "Lo-Fi Beats" and "lo-fi beats" hit the same key.
    /// - maxResults excluded from key: we always request 50 from YouTube and cache all 50,
    ///   then slice to the user's requested count. This maximizes reuse across different values.
    /// - TTL of 1 hour: balances freshness with quota savings per the design spec.
    /// </summary>
    private static string CacheKey(string query) =>
        $"youtube:search:{query.Trim().ToLowerInvariant()}";

    public async Task<Result<SearchVideosResponse>> HandleAsync(SearchVideosRequest request)
    {
        try
        {
            var key = CacheKey(request.Query);
            var requestedMax = request.MaxResults ?? 10;

            if (!cache.TryGetValue(key, out IReadOnlyList<YoutubeVideo>? videos))
            {
                videos = await youtubeClient.SearchVideosAsync(request.Query, MaxYouTubeResults);

                cache.Set(key, videos, TimeSpan.FromHours(CacheDurationHours));
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
        catch (GoogleApiException ex)
        {
            return Result<SearchVideosResponse>.Failure(
                $"Failed to search videos: {ex.Message}");
        }
    }
}