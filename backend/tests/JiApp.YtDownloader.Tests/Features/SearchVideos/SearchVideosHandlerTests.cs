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

    [Fact]
    public async Task HandleAsync_propagates_CancellationToken_to_YoutubeClient()
    {
        var youtubeClient = new Mock<IYoutubeClient>();
        youtubeClient
            .Setup(c => c.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<YoutubeVideo>().AsReadOnly());

        var handler = CreateHandler(youtubeClient: youtubeClient);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        await handler.HandleAsync(new SearchVideosRequest("test", null), token);

        youtubeClient.Verify(
            c => c.SearchVideosAsync("test", It.IsAny<int>(), token),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_returns_sanitized_error_on_GoogleApiException()
    {
        var youtubeClient = new Mock<IYoutubeClient>();
        youtubeClient
            .Setup(c => c.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GoogleApiException("youtube", "sensitive API key details"));

        var handler = CreateHandler(youtubeClient: youtubeClient);

        var result = await handler.HandleAsync(new SearchVideosRequest("test", null));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotContain("sensitive API key details");
        result.Error.Should().Contain("Failed to search videos");
    }
}
