using Google;
using JiApp.Common.Models;
using JiApp.YtApi;
using JiApp.YtDownloader.Agent;
using JiApp.YtDownloader.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.YtDownloader.Tests.Agent;

public sealed class YtAgentToolServiceTests
{
    private const long UserId = 7L;

    private sealed class Fixture
    {
        public Mock<IYoutubeClient> YoutubeClientMock { get; } = new();
        public Mock<ISearchHistoryRepository> SearchHistoryRepoMock { get; } = new();
        public Mock<IDownloadHistoryRepository> DownloadHistoryRepoMock { get; } = new();
        public IMemoryCache Cache { get; } = new MemoryCache(new MemoryCacheOptions());

        public YtAgentToolService Sut =>
            new(
                YoutubeClientMock.Object,
                SearchHistoryRepoMock.Object,
                DownloadHistoryRepoMock.Object,
                Cache,
                Mock.Of<ILogger<YtAgentToolService>>());
    }

    private static YoutubeVideo CreateVideo(string videoId = "dQw4w9WgXcQ", string title = "A title") =>
        new(
            VideoId: videoId,
            Title: title,
            Description: "Official video",
            ImageUrl: $"https://i.ytimg.com/vi/{videoId}/default.jpg",
            ChannelTitle: "Rick Astley");

    [Fact]
    public async Task SearchAsync_WithPlainTextQuery_MapsVideosAndRecordsHistoryWithPassedUserId()
    {
        var fixture = new Fixture();
        fixture.YoutubeClientMock
            .Setup(c => c.SearchVideosAsync("rick astley", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateVideo() });

        YoutubeSearchHistory? recorded = null;
        fixture.SearchHistoryRepoMock
            .Setup(r => r.AddAsync(It.IsAny<YoutubeSearchHistory>()))
            .Callback<YoutubeSearchHistory>(entry => recorded = entry)
            .Returns(Task.CompletedTask);

        var result = await fixture.Sut.SearchAsync(UserId, "rick astley", null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().ContainSingle()
            .Which.VideoId.Should().Be("dQw4w9WgXcQ");

        recorded.Should().NotBeNull();
        recorded!.UserId.Should().Be(UserId);
        recorded.SearchText.Should().Be("rick astley");
        fixture.SearchHistoryRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WithPlainTextQuery_CallsSearchVideosAsync()
    {
        var fixture = new Fixture();
        fixture.YoutubeClientMock
            .Setup(c => c.SearchVideosAsync("never gonna give you up", It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateVideo() });

        var result = await fixture.Sut.SearchAsync(UserId, "never gonna give you up", null);

        result.IsSuccess.Should().BeTrue();
        fixture.YoutubeClientMock.Verify(
            c => c.GetVideoByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.YoutubeClientMock.Verify(
            c => c.SearchVideosAsync("never gonna give you up", It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WithMaxResultsLimit_HonorsLimit()
    {
        var fixture = new Fixture();
        var videos = Enumerable.Range(0, 20)
            .Select(i => CreateVideo($"vid{i:00000000}", $"Title {i}"))
            .ToArray();
        fixture.YoutubeClientMock
            .Setup(c => c.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(videos);

        var result = await fixture.Sut.SearchAsync(UserId, "many results", 3);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().HaveCount(3);
    }

    [Fact]
    public async Task SearchAsync_WithNullMaxResults_DefaultsToTen()
    {
        var fixture = new Fixture();
        var videos = Enumerable.Range(0, 20)
            .Select(i => CreateVideo($"vid{i:00000000}", $"Title {i}"))
            .ToArray();
        fixture.YoutubeClientMock
            .Setup(c => c.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(videos);

        var result = await fixture.Sut.SearchAsync(UserId, "many results", null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().HaveCount(10);
    }

    [Fact]
    public async Task SearchAsync_WithYoutubeUrl_CallsGetVideoByIdAsync()
    {
        const string videoId = "dQw4w9WgXcQ";
        var fixture = new Fixture();
        fixture.YoutubeClientMock
            .Setup(c => c.GetVideoByIdAsync(videoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateVideo(videoId));

        var result = await fixture.Sut.SearchAsync(UserId, $"https://www.youtube.com/watch?v={videoId}", null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().ContainSingle().Which.VideoId.Should().Be(videoId);
        fixture.YoutubeClientMock.Verify(c => c.GetVideoByIdAsync(videoId, It.IsAny<CancellationToken>()), Times.Once);
        fixture.YoutubeClientMock.Verify(
            c => c.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchAsync_PropagatesCancellationToken_ToYoutubeClient()
    {
        var fixture = new Fixture();
        fixture.YoutubeClientMock
            .Setup(c => c.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        await fixture.Sut.SearchAsync(UserId, "test", null, token);

        fixture.YoutubeClientMock.Verify(
            c => c.SearchVideosAsync("test", It.IsAny<int>(), token),
            Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WithGoogleApiException_ReturnsSanitizedFailure()
    {
        var fixture = new Fixture();
        fixture.YoutubeClientMock
            .Setup(c => c.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GoogleApiException("youtube", "sensitive API key details"));

        var result = await fixture.Sut.SearchAsync(UserId, "test", null);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotContain("sensitive API key details");
        result.Error.Should().Contain("Failed to search videos");
    }

    [Fact]
    public async Task SearchAsync_WhenHistoryRecordingThrows_StillSucceeds()
    {
        var fixture = new Fixture();
        fixture.YoutubeClientMock
            .Setup(c => c.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateVideo() });

        fixture.SearchHistoryRepoMock
            .Setup(r => r.AddAsync(It.IsAny<YoutubeSearchHistory>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var result = await fixture.Sut.SearchAsync(UserId, "test", null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().ContainSingle();
    }

    [Fact]
    public async Task ListSearchHistoryAsync_QueriesRepoWithPassedUserId_AndMapsEntities()
    {
        var entity = new YoutubeSearchHistory
        {
            UserId = UserId,
            SearchedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            SearchText = "queen bohemian rhapsody"
        };
        var fixture = new Fixture();
        fixture.SearchHistoryRepoMock
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync([entity]);

        var result = await fixture.Sut.ListSearchHistoryAsync(UserId, null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle()
            .Which.SearchText.Should().Be("queen bohemian rhapsody");
        fixture.SearchHistoryRepoMock.Verify(r => r.GetByUserIdAsync(UserId, 10, It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task ListSearchHistoryAsync_UsesPassedLimit()
    {
        var fixture = new Fixture();
        fixture.SearchHistoryRepoMock
            .Setup(r => r.GetByUserIdAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync([]);

        await fixture.Sut.ListSearchHistoryAsync(UserId, 25);

        fixture.SearchHistoryRepoMock.Verify(r => r.GetByUserIdAsync(UserId, 25, It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task ListDownloadHistoryAsync_QueriesRepoWithPassedUserId_AndMapsEntities()
    {
        var entity = new YoutubeDownloadHistory
        {
            UserId = UserId,
            DownloadedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            VideoTitle = "A song",
            VideoId = "dQw4w9WgXcQ",
            VideoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
        };
        var fixture = new Fixture();
        fixture.DownloadHistoryRepoMock
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync([entity]);

        var result = await fixture.Sut.ListDownloadHistoryAsync(UserId, null);

        result.IsSuccess.Should().BeTrue();
        var item = result.Value!.Items.Should().ContainSingle().Which;
        item.VideoTitle.Should().Be("A song");
        item.VideoId.Should().Be("dQw4w9WgXcQ");
        fixture.DownloadHistoryRepoMock.Verify(r => r.GetByUserIdAsync(UserId, 10, It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task ListDownloadHistoryAsync_UsesPassedLimit()
    {
        var fixture = new Fixture();
        fixture.DownloadHistoryRepoMock
            .Setup(r => r.GetByUserIdAsync(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync([]);

        await fixture.Sut.ListDownloadHistoryAsync(UserId, 25);

        fixture.DownloadHistoryRepoMock.Verify(r => r.GetByUserIdAsync(UserId, 25, It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public void BuildDownloadOffer_ReturnsExpectedRecord_WithNoSideEffects()
    {
        var fixture = new Fixture();

        var offer = fixture.Sut.BuildDownloadOffer(
            UserId,
            "dQw4w9WgXcQ",
            "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            "Never Gonna Give You Up",
            "https://i.ytimg.com/vi/dQw4w9WgXcQ/default.jpg");

        offer.Should().Be(new DownloadOffer(
            "dQw4w9WgXcQ",
            "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            "Never Gonna Give You Up",
            "https://i.ytimg.com/vi/dQw4w9WgXcQ/default.jpg",
            UserId));

        fixture.YoutubeClientMock.VerifyNoOtherCalls();
        fixture.SearchHistoryRepoMock.VerifyNoOtherCalls();
        fixture.DownloadHistoryRepoMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchAsync_DeprioritizesCompilationVideos_BehindIndividualTracks()
    {
        var videos = new YoutubeVideo[]
        {
            CreateVideo("a01", "Top 10 Most Popular Songs by NCS"),
            CreateVideo("a02", "My Individual Track"),
            CreateVideo("a03", "Best of 2025 Mix Compilation"),
            CreateVideo("a04", "Another Single Song"),
            CreateVideo("a05", "Non-Stop Megamix 3 Hours"),
        };

        var fixture = new Fixture();
        fixture.YoutubeClientMock
            .Setup(c => c.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(videos);

        var result = await fixture.Sut.SearchAsync(UserId, "top songs", 5);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().HaveCount(5);

        var titles = result.Value.Results.Select(r => r.Title).ToList();

        var individualIndices = titles
            .Select((title, idx) => (title, idx))
            .Where(x => x.title == "My Individual Track" || x.title == "Another Single Song")
            .Select(x => x.idx)
            .ToList();

        var compilationIndices = titles
            .Select((title, idx) => (title, idx))
            .Where(x => x.title != "My Individual Track" && x.title != "Another Single Song")
            .Select(x => x.idx)
            .ToList();

        individualIndices.Max().Should().BeLessThan(compilationIndices.Min(),
            "individual tracks should appear before compilations");
    }
}
