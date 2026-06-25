using Google;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.YtApi;
using JiApp.YtDownloader.Configuration;
using JiApp.YtDownloader.Features.SearchVideos;
using JiApp.YtDownloader.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.YtDownloader.Tests.Features.SearchVideos;

public sealed class SearchVideosHandlerTests
{
    private const int MaxResults = 6;
    private const int PageSize = 2;

    private static Settings CreateSettings() => new()
    {
        ConnectionString = "Data Source=test.db",
        App = new Settings.AppSettings { BaseDirectory = "/tmp", PreviewDurationSeconds = 10 },
        Jwt = new Settings.JwtSettings { Key = "test-key", Issuer = "test-issuer", Audience = "test-audience" },
        Youtube = new Settings.YoutubeSettings
        {
            ApiKey = "test-key", YtDlpPath = "yt-dlp", FfmpegPath = "ffmpeg",
            MaxResults = MaxResults, PageSize = PageSize,
        },
    };

    private sealed class Fixture
    {
        public Mock<IYoutubeClient> YoutubeClientMock { get; } = new();
        public Mock<ISearchHistoryRepository> HistoryRepoMock { get; } = new();
        public IMemoryCache Cache { get; } = new MemoryCache(new MemoryCacheOptions());
        public SearchVideosHandler Sut { get; }

        public Fixture()
        {
            var user = Mock.Of<ICurrentUserService>(x => x.UserId == 42L && x.Username == "test-user");
            Sut = new SearchVideosHandler(
                YoutubeClientMock.Object, HistoryRepoMock.Object, user, Cache,
                CreateSettings(), Mock.Of<ILogger<SearchVideosHandler>>());
        }

        public Fixture WithSearchResults(params YoutubeVideo[] videos)
        {
            YoutubeClientMock
                .Setup(c => c.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(videos);
            return this;
        }

        public Fixture WithSearchThrows(Exception exception)
        {
            YoutubeClientMock
                .Setup(c => c.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);
            return this;
        }

        public Fixture WithVideoById(string videoId, YoutubeVideo? video)
        {
            YoutubeClientMock
                .Setup(c => c.GetVideoByIdAsync(videoId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(video);
            return this;
        }
    }

    private static YoutubeVideo CreateVideo(string videoId = "dQw4w9WgXcQ") =>
        new(
            VideoId: videoId,
            Title: "Rick Astley - Never Gonna Give You Up",
            Description: "Official video",
            ImageUrl: $"https://i.ytimg.com/vi/{videoId}/default.jpg",
            ChannelTitle: "Rick Astley");

    private static YoutubeVideo[] CreateVideos(int count) =>
        Enumerable.Range(0, count)
            .Select(i => new YoutubeVideo(
                VideoId: $"vid{i:00000000}",
                Title: $"Title {i}",
                Description: $"Description {i}",
                ImageUrl: $"https://i.ytimg.com/vi/vid{i:00000000}/default.jpg",
                ChannelTitle: $"Channel {i}"))
            .ToArray();

    [Fact]
    public async Task HandleAsync_PropagatesCancellationToken_ToYoutubeClient()
    {
        var fixture = new Fixture().WithSearchResults();

        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        await fixture.Sut.HandleAsync(new SearchVideosRequest("test", null), token);

        fixture.YoutubeClientMock.Verify(
            c => c.SearchVideosAsync("test", It.IsAny<int>(), token), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ReturnsSanitizedError_OnGoogleApiException()
    {
        var fixture = new Fixture().WithSearchThrows(new GoogleApiException("youtube", "sensitive API key details"));

        var result = await fixture.Sut.HandleAsync(new SearchVideosRequest("test", null));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotContain("sensitive API key details");
        result.Error.Should().Contain("Failed to search videos");
    }

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
    public async Task HandleAsync_YouTubeUrl_CallsGetVideoByIdAsync(string url, string videoId)
    {
        var fixture = new Fixture().WithVideoById(videoId, CreateVideo(videoId));

        var result = await fixture.Sut.HandleAsync(new SearchVideosRequest(url, null));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().ContainSingle().Which.VideoId.Should().Be(videoId);
        fixture.YoutubeClientMock.Verify(c => c.GetVideoByIdAsync(videoId, It.IsAny<CancellationToken>()), Times.Once);
        fixture.YoutubeClientMock.Verify(
            c => c.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_YouTubeUrl_NotFoundReturnsEmpty()
    {
        const string nonexistentId = "nonexistent1";
        var fixture = new Fixture().WithVideoById(nonexistentId, null);

        var result = await fixture.Sut.HandleAsync(
            new SearchVideosRequest($"https://www.youtube.com/watch?v={nonexistentId}", null));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_PlainTextQuery_CallsSearchVideosAsync()
    {
        var fixture = new Fixture().WithSearchResults(CreateVideo());

        var result = await fixture.Sut.HandleAsync(new SearchVideosRequest("never gonna give you up", null));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().ContainSingle();
        fixture.YoutubeClientMock.Verify(
            c => c.GetVideoByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.YoutubeClientMock.Verify(
            c => c.SearchVideosAsync("never gonna give you up", It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_PlainTextQuery_IsCached()
    {
        var fixture = new Fixture().WithSearchResults(CreateVideo());

        var first = await fixture.Sut.HandleAsync(new SearchVideosRequest("rick astley", null));
        var second = await fixture.Sut.HandleAsync(new SearchVideosRequest("rick astley", null));

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        first.Value!.Results.Should().ContainSingle();
        second.Value!.Results.Should().ContainSingle();
        fixture.YoutubeClientMock.Verify(
            c => c.SearchVideosAsync("rick astley", It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Pagination ─────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Page0_ReturnsFirstPageWithHasMoreTrue()
    {
        var videos = CreateVideos(MaxResults);
        var fixture = new Fixture().WithSearchResults(videos);

        var result = await fixture.Sut.HandleAsync(new SearchVideosRequest("test", 0));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().HaveCount(PageSize);
        result.Value!.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_LastPage_ReturnsHasMoreFalse()
    {
        var videos = CreateVideos(MaxResults);
        var fixture = new Fixture().WithSearchResults(videos);

        var lastPage = (MaxResults / PageSize) - 1;
        var result = await fixture.Sut.HandleAsync(new SearchVideosRequest("test", lastPage));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().HaveCount(PageSize);
        result.Value!.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_NullPage_DefaultsToPage0()
    {
        var videos = CreateVideos(MaxResults);
        var fixture = new Fixture().WithSearchResults(videos);

        var result = await fixture.Sut.HandleAsync(new SearchVideosRequest("test", null));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().HaveCount(PageSize);
        result.Value!.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_PageBeyondEnd_ReturnsEmptyResultsWithHasMoreFalse()
    {
        var videos = CreateVideos(MaxResults);
        var fixture = new Fixture().WithSearchResults(videos);

        var result = await fixture.Sut.HandleAsync(new SearchVideosRequest("test", 999));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Results.Should().BeEmpty();
        result.Value!.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ResultsNeverExceedMaxResults()
    {
        // MaxResults limits what we ask YouTube for; the mock returns all.
        // The paginated traversal across pages should yield exactly MaxResults.
        var videos = CreateVideos(MaxResults);
        var fixture = new Fixture().WithSearchResults(videos);

        var allResults = new List<VideoItem>();
        var page = 0;
        while (true)
        {
            var result = await fixture.Sut.HandleAsync(new SearchVideosRequest("test", page));
            result.IsSuccess.Should().BeTrue();
            allResults.AddRange(result.Value!.Results);
            if (!result.Value!.HasMore)
                break;
            page++;
        }

        allResults.Should().HaveCount(MaxResults);
    }

    [Fact]
    public async Task HandleAsync_SearchHistory_WrittenOnPage0()
    {
        var fixture = new Fixture().WithSearchResults(CreateVideos(1));

        await fixture.Sut.HandleAsync(new SearchVideosRequest("history test", 0));

        fixture.HistoryRepoMock.Verify(r => r.AddAsync(It.IsAny<YoutubeSearchHistory>()), Times.Once);
        fixture.HistoryRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_SearchHistory_NotWrittenOnPageGreaterThanZero()
    {
        var videos = CreateVideos(MaxResults);
        var fixture = new Fixture().WithSearchResults(videos);

        await fixture.Sut.HandleAsync(new SearchVideosRequest("history test", 1));

        fixture.HistoryRepoMock.Verify(r => r.AddAsync(It.IsAny<YoutubeSearchHistory>()), Times.Never);
        fixture.HistoryRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}
