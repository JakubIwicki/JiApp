using Google;
using JiApp.Common.Models;
using JiApp.YtApi;
using JiApp.YtDownloader.Agent;
using JiApp.YtDownloader.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.YtDownloader.Tests.Agent;

public class YtAgentToolServiceTests
{
    private const long UserId = 7L;

    private static YtAgentToolService CreateService(
        Mock<IYoutubeClient>? youtubeClient = null,
        Mock<ISearchHistoryRepository>? searchHistoryRepo = null,
        Mock<IDownloadHistoryRepository>? downloadHistoryRepo = null,
        IMemoryCache? cache = null)
    {
        var yt = youtubeClient ?? new Mock<IYoutubeClient>();
        var searchRepo = searchHistoryRepo ?? new Mock<ISearchHistoryRepository>();
        var downloadRepo = downloadHistoryRepo ?? new Mock<IDownloadHistoryRepository>();
        var memCache = cache ?? new MemoryCache(new MemoryCacheOptions());
        var logger = Mock.Of<ILogger<YtAgentToolService>>();

        return new YtAgentToolService(yt.Object, searchRepo.Object, downloadRepo.Object, memCache, logger);
    }

    private static YoutubeVideo CreateVideo(string videoId = "dQw4w9WgXcQ", string title = "A title") =>
        new(
            VideoId: videoId,
            Title: title,
            Description: "Official video",
            ImageUrl: $"https://i.ytimg.com/vi/{videoId}/default.jpg",
            ChannelTitle: "Rick Astley");

    // ── SearchAsync: mapping + history with the passed userId ───────────────

    [Fact]
    public async Task SearchAsync_maps_videos_and_records_history_with_passed_userId()
    {
        // Arrange
        var youtubeClient = new Mock<IYoutubeClient>();
        youtubeClient
            .Setup(c => c.SearchVideosAsync("rick astley", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateVideo() });

        var searchHistoryRepo = new Mock<ISearchHistoryRepository>();
        YoutubeSearchHistory? recorded = null;
        searchHistoryRepo
            .Setup(r => r.AddAsync(It.IsAny<YoutubeSearchHistory>()))
            .Callback<YoutubeSearchHistory>(entry => recorded = entry)
            .Returns(Task.CompletedTask);

        var sut = CreateService(youtubeClient: youtubeClient, searchHistoryRepo: searchHistoryRepo);

        // Act
        var result = await sut.SearchAsync(UserId, "rick astley", null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().ContainSingle()
            .Which.VideoId.Should().Be("dQw4w9WgXcQ");

        recorded.Should().NotBeNull();
        recorded!.UserId.Should().Be(UserId);
        recorded.SearchText.Should().Be("rick astley");
        searchHistoryRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_plain_text_query_calls_SearchVideosAsync()
    {
        // Arrange
        var youtubeClient = new Mock<IYoutubeClient>();
        youtubeClient
            .Setup(c => c.SearchVideosAsync("never gonna give you up", It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateVideo() });

        var sut = CreateService(youtubeClient: youtubeClient);

        // Act
        var result = await sut.SearchAsync(UserId, "never gonna give you up", null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        youtubeClient.Verify(
            c => c.GetVideoByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        youtubeClient.Verify(
            c => c.SearchVideosAsync("never gonna give you up", It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchAsync_honors_maxResults_limit()
    {
        // Arrange
        var youtubeClient = new Mock<IYoutubeClient>();
        var videos = Enumerable.Range(0, 20)
            .Select(i => CreateVideo($"vid{i:00000000}", $"Title {i}"))
            .ToArray();
        youtubeClient
            .Setup(c => c.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(videos);

        var sut = CreateService(youtubeClient: youtubeClient);

        // Act
        var result = await sut.SearchAsync(UserId, "many results", 3);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().HaveCount(3);
    }

    [Fact]
    public async Task SearchAsync_default_maxResults_is_ten()
    {
        // Arrange
        var youtubeClient = new Mock<IYoutubeClient>();
        var videos = Enumerable.Range(0, 20)
            .Select(i => CreateVideo($"vid{i:00000000}", $"Title {i}"))
            .ToArray();
        youtubeClient
            .Setup(c => c.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(videos);

        var sut = CreateService(youtubeClient: youtubeClient);

        // Act
        var result = await sut.SearchAsync(UserId, "many results", null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().HaveCount(10);
    }

    [Fact]
    public async Task SearchAsync_youtube_url_calls_GetVideoByIdAsync()
    {
        // Arrange
        const string videoId = "dQw4w9WgXcQ";
        var youtubeClient = new Mock<IYoutubeClient>();
        youtubeClient
            .Setup(c => c.GetVideoByIdAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateVideo(videoId));

        var sut = CreateService(youtubeClient: youtubeClient);

        // Act
        var result = await sut.SearchAsync(UserId, $"https://www.youtube.com/watch?v={videoId}", null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().ContainSingle().Which.VideoId.Should().Be(videoId);
        youtubeClient.Verify(c => c.GetVideoByIdAsync(videoId, It.IsAny<CancellationToken>()), Times.Once);
        youtubeClient.Verify(
            c => c.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchAsync_propagates_CancellationToken_to_YoutubeClient()
    {
        // Arrange
        var youtubeClient = new Mock<IYoutubeClient>();
        youtubeClient
            .Setup(c => c.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var sut = CreateService(youtubeClient: youtubeClient);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await sut.SearchAsync(UserId, "test", null, token);

        // Assert
        youtubeClient.Verify(
            c => c.SearchVideosAsync("test", It.IsAny<int>(), token),
            Times.Once);
    }

    [Fact]
    public async Task SearchAsync_returns_sanitized_failure_on_GoogleApiException()
    {
        // Arrange
        var youtubeClient = new Mock<IYoutubeClient>();
        youtubeClient
            .Setup(c => c.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GoogleApiException("youtube", "sensitive API key details"));

        var sut = CreateService(youtubeClient: youtubeClient);

        // Act
        var result = await sut.SearchAsync(UserId, "test", null);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotContain("sensitive API key details");
        result.Error.Should().Contain("Failed to search videos");
    }

    [Fact]
    public async Task SearchAsync_succeeds_even_when_history_recording_throws()
    {
        // Arrange
        var youtubeClient = new Mock<IYoutubeClient>();
        youtubeClient
            .Setup(c => c.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateVideo() });

        var searchHistoryRepo = new Mock<ISearchHistoryRepository>();
        searchHistoryRepo
            .Setup(r => r.AddAsync(It.IsAny<YoutubeSearchHistory>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var sut = CreateService(youtubeClient: youtubeClient, searchHistoryRepo: searchHistoryRepo);

        // Act
        var result = await sut.SearchAsync(UserId, "test", null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().ContainSingle();
    }

    // ── ListSearchHistoryAsync ─────────────────────────────────────────────

    [Fact]
    public async Task ListSearchHistoryAsync_queries_repo_with_passed_userId_and_maps_entities()
    {
        // Arrange
        var entity = new YoutubeSearchHistory
        {
            UserId = UserId,
            SearchedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            SearchText = "queen bohemian rhapsody"
        };
        var searchHistoryRepo = new Mock<ISearchHistoryRepository>();
        searchHistoryRepo
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync([entity]);

        var sut = CreateService(searchHistoryRepo: searchHistoryRepo);

        // Act
        var result = await sut.ListSearchHistoryAsync(UserId, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle()
            .Which.SearchText.Should().Be("queen bohemian rhapsody");
        searchHistoryRepo.Verify(r => r.GetByUserIdAsync(UserId, 10, It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task ListSearchHistoryAsync_uses_passed_limit()
    {
        // Arrange
        var searchHistoryRepo = new Mock<ISearchHistoryRepository>();
        searchHistoryRepo
            .Setup(r => r.GetByUserIdAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync([]);

        var sut = CreateService(searchHistoryRepo: searchHistoryRepo);

        // Act
        await sut.ListSearchHistoryAsync(UserId, 25);

        // Assert
        searchHistoryRepo.Verify(r => r.GetByUserIdAsync(UserId, 25, It.IsAny<int>()), Times.Once);
    }

    // ── ListDownloadHistoryAsync ───────────────────────────────────────────

    [Fact]
    public async Task ListDownloadHistoryAsync_queries_repo_with_passed_userId_and_maps_entities()
    {
        // Arrange
        var entity = new YoutubeDownloadHistory
        {
            UserId = UserId,
            DownloadedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            VideoTitle = "A song",
            VideoId = "dQw4w9WgXcQ",
            VideoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
        };
        var downloadHistoryRepo = new Mock<IDownloadHistoryRepository>();
        downloadHistoryRepo
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync([entity]);

        var sut = CreateService(downloadHistoryRepo: downloadHistoryRepo);

        // Act
        var result = await sut.ListDownloadHistoryAsync(UserId, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var item = result.Value!.Items.Should().ContainSingle().Which;
        item.VideoTitle.Should().Be("A song");
        item.VideoId.Should().Be("dQw4w9WgXcQ");
        downloadHistoryRepo.Verify(r => r.GetByUserIdAsync(UserId, 10, It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task ListDownloadHistoryAsync_uses_passed_limit()
    {
        // Arrange
        var downloadHistoryRepo = new Mock<IDownloadHistoryRepository>();
        downloadHistoryRepo
            .Setup(r => r.GetByUserIdAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync([]);

        var sut = CreateService(downloadHistoryRepo: downloadHistoryRepo);

        // Act
        await sut.ListDownloadHistoryAsync(UserId, 25);

        // Assert
        downloadHistoryRepo.Verify(r => r.GetByUserIdAsync(UserId, 25, It.IsAny<int>()), Times.Once);
    }

    // ── BuildDownloadOffer: pure, no side effects ──────────────────────────

    [Fact]
    public void BuildDownloadOffer_returns_expected_record_with_no_side_effects()
    {
        // Arrange
        var youtubeClient = new Mock<IYoutubeClient>(MockBehavior.Strict);
        var searchHistoryRepo = new Mock<ISearchHistoryRepository>(MockBehavior.Strict);
        var downloadHistoryRepo = new Mock<IDownloadHistoryRepository>(MockBehavior.Strict);

        var sut = CreateService(
            youtubeClient: youtubeClient,
            searchHistoryRepo: searchHistoryRepo,
            downloadHistoryRepo: downloadHistoryRepo);

        // Act
        var offer = sut.BuildDownloadOffer(
            UserId,
            "dQw4w9WgXcQ",
            "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            "Never Gonna Give You Up",
            "https://i.ytimg.com/vi/dQw4w9WgXcQ/default.jpg");

        // Assert
        offer.Should().Be(new DownloadOffer(
            "dQw4w9WgXcQ",
            "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            "Never Gonna Give You Up",
            "https://i.ytimg.com/vi/dQw4w9WgXcQ/default.jpg",
            UserId));

        youtubeClient.VerifyNoOtherCalls();
        searchHistoryRepo.VerifyNoOtherCalls();
        downloadHistoryRepo.VerifyNoOtherCalls();
    }
}
