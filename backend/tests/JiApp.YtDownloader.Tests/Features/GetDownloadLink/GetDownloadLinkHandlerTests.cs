using JiApp.Common.Abstractions;
using JiApp.YtApi;
using JiApp.YtDownloader.Configuration;
using JiApp.YtDownloader.Features.GetDownloadLink;
using JiApp.YtDownloader.Repositories;
using JiApp.YtDownloader.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.YtDownloader.Tests.Features.GetDownloadLink;

public sealed class GetDownloadLinkHandlerTests
{
    private sealed class Fixture
    {
        public Mock<IYoutubeClient> YoutubeClientMock { get; } = new();
        public Mock<ITempFileStore> TempFileStoreMock { get; } = new();
        public Mock<IDownloadHistoryRepository> HistoryRepoMock { get; } = new();
        public GetDownloadLinkHandler Sut { get; }

        public Fixture()
        {
            var user = Mock.Of<ICurrentUserService>(x => x.UserId == 42L && x.Username == "test-user");
            var settings = new Settings
            {
                App = new Settings.AppSettings { BaseDirectory = "/tmp" },
                Youtube = new Settings.YoutubeSettings()
            };
            Sut = new GetDownloadLinkHandler(
                YoutubeClientMock.Object, TempFileStoreMock.Object, HistoryRepoMock.Object, user, settings, Mock.Of<ILogger<GetDownloadLinkHandler>>());
        }

        public Fixture WithDownloadThrows(Exception exception)
        {
            YoutubeClientMock
                .Setup(c => c.DownloadVideoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);
            return this;
        }
    }

    [Fact]
    public async Task HandleAsync_ReturnsSanitizedError_OnException()
    {
        var fixture = new Fixture().WithDownloadThrows(new InvalidOperationException("sensitive yt-dlp error details"));

        var request = new DownloadRequest("dQw4w9WgXcQ", "https://youtube.com/watch?v=dQw4w9WgXcQ",
            "Title", "Description", "https://example.com/img.jpg");
        var result = await fixture.Sut.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotContain("sensitive yt-dlp error details");
        result.Error.Should().Contain("Failed to process download");
    }
}
