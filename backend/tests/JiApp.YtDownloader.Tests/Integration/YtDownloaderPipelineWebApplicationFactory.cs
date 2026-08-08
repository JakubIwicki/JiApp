using JiApp.Testing.Common.Bases;
using JiApp.Testing.Common.Fakes;
using JiApp.YtApi.Clients;
using JiApp.YtApi.Contracts;
using JiApp.YtDownloader.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace JiApp.YtDownloader.Tests.Integration;

/// <summary>
/// Full-pipeline host for the YtDownloader module on the shared in-memory SQLite
/// store. The background workers (DownloadWorker, TempFileCleanupService) are
/// removed — the worker would spawn yt-dlp processes and the cleanup would delete
/// temp files — and IYoutubeClient is replaced with a Moq stub so no YouTube API or
/// yt-dlp call ever leaves the process. TimeProvider is pinned to a fixed now for
/// deterministic history timestamps and download TTLs.
/// </summary>
public sealed class YtDownloaderPipelineWebApplicationFactory
    : ModuleSqliteIntegrationTestBase<JiApp.YtDownloader.Program, YtDbContext>
{
    public Mock<IYoutubeClient> YoutubeClientMock { get; } = CreateYoutubeClientMock();

    protected override void ConfigureModuleServices(IServiceCollection services)
    {
        RemoveAllHostedServices(services);

        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2030, 1, 15, 0, 0, 0, TimeSpan.Zero));
        services.RemoveAll<TimeProvider>();
        services.AddSingleton<TimeProvider>(fakeTime);

        services.RemoveAll<IYoutubeClient>();
        services.AddSingleton(YoutubeClientMock.Object);
    }

    private static Mock<IYoutubeClient> CreateYoutubeClientMock()
    {
        var mock = new Mock<IYoutubeClient>();

        mock.Setup(c => c.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<YoutubeVideo>
            {
                new("dQw4w9WgXcQ", "Test Video", "Test Description",
                    "https://img.youtube.com/vi/dQw4w9WgXcQ/default.jpg", "Test Channel")
            });
        mock.Setup(c => c.GetVideoByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("not expected in these tests"));
        mock.Setup(c => c.DownloadVideoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("not expected in these tests"));
        mock.Setup(c => c.BuildPreviewAudioProcess(It.IsAny<string>()))
            .Throws(new InvalidOperationException("not expected in these tests"));

        return mock;
    }
}
