using JiApp.Common.Abstractions;
using JiApp.YtApi;
using JiApp.YtDownloader.Configuration;
using JiApp.YtDownloader.Features.GetDownloadLink;
using JiApp.YtDownloader.Repositories;
using JiApp.YtDownloader.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.YtDownloader.Tests.Features.GetDownloadLink;

public class GetDownloadLinkHandlerTests
{
    private sealed class FakeCurrentUser : ICurrentUserService
    {
        public long UserId => 42L;
        public string Username => "test-user";
    }

    private static GetDownloadLinkHandler CreateHandler(
        Mock<IYoutubeClient>? youtubeClient = null,
        Mock<ITempFileStore>? tempFileStore = null,
        Mock<IDownloadHistoryRepository>? historyRepo = null,
        Settings? settings = null)
    {
        var yt = youtubeClient ?? new Mock<IYoutubeClient>();
        var store = tempFileStore ?? new Mock<ITempFileStore>();
        var repo = historyRepo ?? new Mock<IDownloadHistoryRepository>();
        var user = new FakeCurrentUser();
        var s = settings ?? new Settings
        {
            App = new Settings.AppSettings { BaseDirectory = "/tmp" },
            Youtube = new Settings.YoutubeSettings()
        };
        var logger = Mock.Of<ILogger<GetDownloadLinkHandler>>();

        return new GetDownloadLinkHandler(yt.Object, store.Object, repo.Object, user, s, logger);
    }

    [Fact]
    public async Task HandleAsync_returns_sanitized_error_on_exception()
    {
        var youtubeClient = new Mock<IYoutubeClient>();
        youtubeClient
            .Setup(c => c.DownloadVideoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sensitive yt-dlp error details"));

        var handler = CreateHandler(youtubeClient: youtubeClient);

        var request = new DownloadRequest("dQw4w9WgXcQ", "https://youtube.com/watch?v=dQw4w9WgXcQ",
            "Title", "Description", "https://example.com/img.jpg");
        var result = await handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotContain("sensitive yt-dlp error details");
        result.Error.Should().Contain("Failed to process download");
    }
}
