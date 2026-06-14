using Google;
using JiApp.Common.Abstractions;
using JiApp.YtApi;
using JiApp.YtDownloader.Features.SearchVideos;
using JiApp.YtDownloader.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.YtDownloader.Tests.Features.SearchVideos;

public class SearchVideosHandlerTests
{
    private sealed class FakeCurrentUser : ICurrentUserService
    {
        public long UserId => 42L;
        public string Username => "test-user";
    }

    private static SearchVideosHandler CreateHandler(
        Mock<IYoutubeClient>? youtubeClient = null,
        Mock<ISearchHistoryRepository>? historyRepo = null,
        IMemoryCache? cache = null)
    {
        var yt = youtubeClient ?? new Mock<IYoutubeClient>();
        var repo = historyRepo ?? new Mock<ISearchHistoryRepository>();
        var user = new FakeCurrentUser();
        var memCache = cache ?? new MemoryCache(new MemoryCacheOptions());
        var logger = Mock.Of<ILogger<SearchVideosHandler>>();

        return new SearchVideosHandler(yt.Object, repo.Object, user, memCache, logger);
    }

    private static YoutubeVideo CreateVideo(string videoId = "dQw4w9WgXcQ") =>
        new(
            VideoId: videoId,
            Title: "Rick Astley - Never Gonna Give You Up",
            Description: "Official video",
            ImageUrl: $"https://i.ytimg.com/vi/{videoId}/default.jpg",
            ChannelTitle: "Rick Astley");

    // ── Cancellation ───────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_propagates_CancellationToken_to_YoutubeClient()
    {
        // Arrange
        var youtubeClient = new Mock<IYoutubeClient>();
        youtubeClient
            .Setup(c => c.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = CreateHandler(youtubeClient: youtubeClient);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await handler.HandleAsync(new SearchVideosRequest("test", null), token);

        // Assert
        youtubeClient.Verify(
            c => c.SearchVideosAsync("test", It.IsAny<int>(), token),
            Times.Once);
    }

    // ── Error handling ─────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_returns_sanitized_error_on_GoogleApiException()
    {
        // Arrange
        var youtubeClient = new Mock<IYoutubeClient>();
        youtubeClient
            .Setup(c => c.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GoogleApiException("youtube", "sensitive API key details"));

        var handler = CreateHandler(youtubeClient: youtubeClient);

        // Act
        var result = await handler.HandleAsync(new SearchVideosRequest("test", null));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotContain("sensitive API key details");
        result.Error.Should().Contain("Failed to search videos");
    }

    // ── YouTube URL detection ──────────────────────────────────────────────

    public static TheoryData<string, string> YoutubeUrlCases =>
        new()
        {
            { "https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ" },
            { "https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ" },
            { "https://www.youtube.com/shorts/abc123def45", "abc123def45" },
            { "https://www.youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ" },
            { "https://m.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ" },
        };

    [Theory]
    [MemberData(nameof(YoutubeUrlCases))]
    public async Task HandleAsync_youtube_url_calls_GetVideoByIdAsync(
        string url, string videoId)
    {
        // Arrange
        var youtubeClient = new Mock<IYoutubeClient>();
        youtubeClient
            .Setup(c => c.GetVideoByIdAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateVideo(videoId));

        var handler = CreateHandler(youtubeClient: youtubeClient);

        // Act
        var result = await handler.HandleAsync(new SearchVideosRequest(url, null));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().ContainSingle().Which.VideoId.Should().Be(videoId);
        youtubeClient.Verify(c => c.GetVideoByIdAsync(videoId, It.IsAny<CancellationToken>()), Times.Once);
        youtubeClient.Verify(
            c => c.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_youtube_url_not_found_returns_empty()
    {
        // Arrange
        const string nonexistentId = "nonexistent1";
        var youtubeClient = new Mock<IYoutubeClient>();
        youtubeClient
            .Setup(c => c.GetVideoByIdAsync(nonexistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((YoutubeVideo?)null);

        var handler = CreateHandler(youtubeClient: youtubeClient);

        // Act
        var result = await handler.HandleAsync(
            new SearchVideosRequest($"https://www.youtube.com/watch?v={nonexistentId}", null));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().BeEmpty();
    }

    // ── Plain text search ──────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_plain_text_query_calls_SearchVideosAsync()
    {
        // Arrange
        var youtubeClient = new Mock<IYoutubeClient>();
        youtubeClient
            .Setup(c => c.SearchVideosAsync(
                "never gonna give you up", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateVideo() });

        var handler = CreateHandler(youtubeClient: youtubeClient);

        // Act
        var result = await handler.HandleAsync(
            new SearchVideosRequest("never gonna give you up", null));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().ContainSingle();
        youtubeClient.Verify(
            c => c.GetVideoByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        youtubeClient.Verify(
            c => c.SearchVideosAsync(
                "never gonna give you up", It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_plain_text_query_is_cached()
    {
        // Arrange
        var youtubeClient = new Mock<IYoutubeClient>();
        youtubeClient
            .Setup(c => c.SearchVideosAsync(
                "rick astley", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateVideo() });

        var handler = CreateHandler(youtubeClient: youtubeClient);

        // Act
        var first = await handler.HandleAsync(new SearchVideosRequest("rick astley", null));
        var second = await handler.HandleAsync(new SearchVideosRequest("rick astley", null));

        // Assert
        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        first.Value!.Results.Should().ContainSingle();
        second.Value!.Results.Should().ContainSingle();

        // SearchVideosAsync should only have been called once — the second
        // call hits the in-memory cache.
        youtubeClient.Verify(
            c => c.SearchVideosAsync(
                "rick astley", It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
