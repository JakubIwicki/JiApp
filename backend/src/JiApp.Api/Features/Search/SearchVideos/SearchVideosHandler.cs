using Google;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Infrastructure.Repositories;
using JiApp.YtApi;

namespace JiApp.Api.Features.Search.SearchVideos;

public sealed class SearchVideosHandler
{
    private readonly IYoutubeClient _youtubeClient;
    private readonly ISearchHistoryRepository _searchHistoryRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<SearchVideosHandler> _logger;

    public SearchVideosHandler(
        IYoutubeClient youtubeClient,
        ISearchHistoryRepository searchHistoryRepository,
        ICurrentUserService currentUser,
        ILogger<SearchVideosHandler> logger)
    {
        _youtubeClient = youtubeClient;
        _searchHistoryRepository = searchHistoryRepository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<SearchVideosResponse>> HandleAsync(SearchVideosRequest request)
    {
        try
        {
            var maxResults = request.MaxResults ?? 10;

            var videos = await _youtubeClient.SearchVideosAsync(request.Query, maxResults);

            var historyEntry = new YoutubeSearchHistory
            {
                UserId = _currentUser.UserId,
                SearchedAt = DateTime.UtcNow,
                SearchText = request.Query
            };

            try
            {
                await _searchHistoryRepository.AddAsync(historyEntry);
                await _searchHistoryRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save search history for user {UserId}", _currentUser.UserId);
            }

            var items = videos.Select(v => new VideoItem(
                v.VideoId,
                v.Title,
                v.Description,
                v.ImageUrl,
                v.VideoUrl
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
