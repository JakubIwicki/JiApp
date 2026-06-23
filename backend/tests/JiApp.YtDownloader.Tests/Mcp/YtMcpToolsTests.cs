using JiApp.Common.Abstractions;
using JiApp.YtApi;
using JiApp.YtDownloader.Agent;
using JiApp.YtDownloader.Mcp;
using JiApp.YtDownloader.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.YtDownloader.Tests.Mcp;

public class YtMcpToolsTests
{
    private const long UserId = 42L;

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

    private static ICurrentUserService CreateCurrentUser(long userId = UserId)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(c => c.UserId).Returns(userId);
        return mock.Object;
    }

    private static YoutubeVideo CreateVideo(string videoId = "dQw4w9WgXcQ", string title = "A title") =>
        new(
            VideoId: videoId,
            Title: title,
            Description: "Official video",
            ImageUrl: $"https://i.ytimg.com/vi/{videoId}/default.jpg",
            ChannelTitle: "Rick Astley");

    // ── SearchYoutube: delegates to YtAgentToolService.SearchAsync ───────────

    [Fact]
    public async Task SearchYoutube_ReturnsMappedVideos_ForQuery()
    {
        // Arrange
        var youtubeClient = new Mock<IYoutubeClient>();
        youtubeClient
            .Setup(c => c.SearchVideosAsync("lofi", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateVideo()]);

        var sut = CreateService(youtubeClient: youtubeClient);
        var currentUser = CreateCurrentUser();

        // Act
        var results = await YtMcpTools.SearchYoutube(
            sut, currentUser, "lofi", null, CancellationToken.None);

        // Assert
        var video = results.Should().ContainSingle().Which;
        video.VideoId.Should().Be("dQw4w9WgXcQ");
        video.Title.Should().Be("A title");
    }

    // ── OfferDownload: pure factory, no download/network calls ─────────────

    [Fact]
    public void OfferDownload_ReturnsOffer_WithoutDownloading()
    {
        // Arrange
        var youtubeClient = new Mock<IYoutubeClient>(MockBehavior.Strict);
        var searchHistoryRepo = new Mock<ISearchHistoryRepository>(MockBehavior.Strict);
        var downloadHistoryRepo = new Mock<IDownloadHistoryRepository>(MockBehavior.Strict);

        var sut = CreateService(
            youtubeClient: youtubeClient,
            searchHistoryRepo: searchHistoryRepo,
            downloadHistoryRepo: downloadHistoryRepo);
        var currentUser = CreateCurrentUser();

        // Act
        var offer = YtMcpTools.OfferDownload(
            sut, currentUser, "vid123", "https://youtu.be/vid123", "Title", null);

        // Assert
        offer.VideoId.Should().Be("vid123");
        offer.VideoUrl.Should().Be("https://youtu.be/vid123");
        offer.Title.Should().Be("Title");
        offer.ImageUrl.Should().BeNull();

        youtubeClient.VerifyNoOtherCalls();
        searchHistoryRepo.VerifyNoOtherCalls();
        downloadHistoryRepo.VerifyNoOtherCalls();
    }
}
