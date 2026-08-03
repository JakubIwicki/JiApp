using System.Diagnostics;
using System.Threading.Channels;
using JiApp.Common.Models;
using JiApp.YtApi;
using JiApp.YtDownloader.Configuration;
using JiApp.YtDownloader.Repositories;
using JiApp.YtDownloader.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JiApp.YtDownloader.Tests.Services;

public sealed class DownloadWorkerTests
{
    private const long UserId = 7L;
    private const string VideoId = "dQw4w9WgXcQ";
    private const string VideoUrl = "https://youtube.com/watch?v=dQw4w9WgXcQ";
    private static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(10);

    private sealed class Fixture : IDisposable
    {
        public DownloadJobStore JobStore { get; }
        public Channel<string> Queue { get; } = Channel.CreateUnbounded<string>();
        public Mock<IYoutubeClient> YoutubeClientMock { get; } = new();
        public Mock<IDownloadHistoryRepository> HistoryRepoMock { get; } = new();
        public DownloadWorker Sut { get; }
        public string TempDir { get; }

        public Fixture(TimeProvider? timeProvider = null, TimeSpan? downloadTimeout = null)
        {
            JobStore = new DownloadJobStore(TimeSpan.FromMinutes(15));
            TempDir = Directory.CreateTempSubdirectory("ytdl-worker-tests-").FullName;
            var settings = new Settings { App = new Settings.AppSettings { BaseDirectory = TempDir } };

            var services = new ServiceCollection();
            services.AddScoped(_ => HistoryRepoMock.Object);
            var provider = services.BuildServiceProvider();

            Sut = new DownloadWorker(
                JobStore,
                Queue,
                YoutubeClientMock.Object,
                provider.GetRequiredService<IServiceScopeFactory>(),
                settings,
                NullLogger<DownloadWorker>.Instance,
                timeProvider,
                downloadTimeout);
        }

        public string CreateJob(string videoId = VideoId) =>
            JobStore.CreateJob(UserId, videoId, "Title", "Description",
                "https://example.com/i.jpg", VideoUrl);

        public string CreateReadyFile()
        {
            var path = Path.Combine(TempDir, $"{Guid.NewGuid():N}.mp3");
            File.WriteAllBytes(path, [0x49, 0x44, 0x33]);
            return path;
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(TempDir, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now) => _now = now;

        public override DateTimeOffset GetUtcNow() => _now;
    }

    private static async Task<DownloadJobStatusResult> RunToTerminalStatusAsync(Fixture fixture, string tempId)
    {
        await fixture.Sut.StartAsync(CancellationToken.None);
        fixture.Queue.Writer.TryWrite(tempId);
        try
        {
            return await WaitForTerminalStatusAsync(fixture.JobStore, tempId);
        }
        finally
        {
            await fixture.Sut.StopAsync(CancellationToken.None);
        }
    }

    private static async Task<DownloadJobStatusResult> WaitForTerminalStatusAsync(DownloadJobStore store, string tempId)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < PollTimeout)
        {
            var status = store.GetStatus(tempId, UserId);
            if (status is { Status: DownloadJobStatus.Ready or DownloadJobStatus.Failed })
                return status;

            await Task.Delay(50);
        }

        throw new TimeoutException($"Job {tempId} did not reach a terminal state within {PollTimeout}.");
    }

    [Fact]
    public async Task MarksJobReady_AndWritesHistory_WhenDownloadSucceeds()
    {
        using var fixture = new Fixture();
        var filePath = fixture.CreateReadyFile();
        fixture.YoutubeClientMock
            .Setup(c => c.DownloadVideoAsync(VideoId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new YoutubeClientResponse(filePath, true, []));
        var tempId = fixture.CreateJob();

        var status = await RunToTerminalStatusAsync(fixture, tempId);

        status.Status.Should().Be(DownloadJobStatus.Ready);
        fixture.JobStore.GetFilePath(tempId, UserId).Should().Be(filePath);
        fixture.HistoryRepoMock.Verify(
            r => r.AddAsync(It.Is<YoutubeDownloadHistory>(h =>
                h.UserId == UserId && h.VideoId == VideoId && h.VideoTitle == "Title" &&
                h.VideoDescription == "Description" && h.VideoUrl == VideoUrl)),
            Times.Once);
        fixture.HistoryRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task MarksJobFailed_WithYoutubeDlCategory_WhenYtDlpFails()
    {
        using var fixture = new Fixture();
        fixture.YoutubeClientMock
            .Setup(c => c.DownloadVideoAsync(VideoId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new YoutubeClientResponse(null, false, ["Sign in to confirm you're not a bot"]));
        var tempId = fixture.CreateJob();

        var status = await RunToTerminalStatusAsync(fixture, tempId);

        status.Status.Should().Be(DownloadJobStatus.Failed);
        status.ErrorCategory.Should().Be(DownloadWorker.YoutubeDlErrorCategory);
        status.Error.Should().NotBeNullOrWhiteSpace();
        fixture.HistoryRepoMock.Verify(r => r.AddAsync(It.IsAny<YoutubeDownloadHistory>()), Times.Never);
        fixture.HistoryRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task MarksJobFailed_WithSanitizedError_WhenYoutubeClientThrows()
    {
        using var fixture = new Fixture();
        fixture.YoutubeClientMock
            .Setup(c => c.DownloadVideoAsync(VideoId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sensitive yt-dlp error details"));
        var tempId = fixture.CreateJob();

        var status = await RunToTerminalStatusAsync(fixture, tempId);

        status.Status.Should().Be(DownloadJobStatus.Failed);
        status.Error.Should().NotContain("sensitive yt-dlp error details");
        status.Error.Should().Contain("Failed to process download");
        fixture.HistoryRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task MarksJobFailed_WhenFileMissingDespiteSuccess()
    {
        using var fixture = new Fixture();
        fixture.YoutubeClientMock
            .Setup(c => c.DownloadVideoAsync(VideoId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new YoutubeClientResponse(null, true, []));
        var tempId = fixture.CreateJob();

        var status = await RunToTerminalStatusAsync(fixture, tempId);

        status.Status.Should().Be(DownloadJobStatus.Failed);
        status.ErrorCategory.Should().Be(DownloadWorker.YoutubeDlErrorCategory);
        status.Error.Should().Contain("file is missing");
    }

    [Fact]
    public async Task MarksJobFailed_WhenFileIsEmpty()
    {
        using var fixture = new Fixture();
        var emptyFile = Path.Combine(fixture.TempDir, "empty.mp3");
        File.WriteAllBytes(emptyFile, []);
        fixture.YoutubeClientMock
            .Setup(c => c.DownloadVideoAsync(VideoId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new YoutubeClientResponse(emptyFile, true, []));
        var tempId = fixture.CreateJob();

        var status = await RunToTerminalStatusAsync(fixture, tempId);

        status.Status.Should().Be(DownloadJobStatus.Failed);
        status.ErrorCategory.Should().Be(DownloadWorker.YoutubeDlErrorCategory);
        status.Error.Should().Contain("file is empty");
    }

    [Fact]
    public async Task MarksJobFailed_AsTimedOut_WhenDownloadExceedsTimeout()
    {
        using var fixture = new Fixture(downloadTimeout: TimeSpan.FromSeconds(1));
        fixture.YoutubeClientMock
            .Setup(c => c.DownloadVideoAsync(VideoId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, string _, CancellationToken ct) =>
            {
                await Task.Delay(System.Threading.Timeout.Infinite, ct);
                return new YoutubeClientResponse(null, true, []);
            });
        var tempId = fixture.CreateJob();

        var status = await RunToTerminalStatusAsync(fixture, tempId);

        status.Status.Should().Be(DownloadJobStatus.Failed);
        status.Error.Should().Be("Download timed out.");
    }

    [Fact]
    public async Task WritesHistory_WhenRepoFailsTwiceThenSucceeds()
    {
        using var fixture = new Fixture();
        var filePath = fixture.CreateReadyFile();
        fixture.YoutubeClientMock
            .Setup(c => c.DownloadVideoAsync(VideoId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new YoutubeClientResponse(filePath, true, []));

        var attempts = 0;
        fixture.HistoryRepoMock
            .Setup(r => r.SaveChangesAsync())
            .Returns(async () =>
            {
                attempts++;
                if (attempts <= 2)
                    throw new InvalidOperationException("database unavailable");
            });
        var tempId = fixture.CreateJob();

        var status = await RunToTerminalStatusAsync(fixture, tempId);

        status.Status.Should().Be(DownloadJobStatus.Ready);
        fixture.HistoryRepoMock.Verify(r => r.AddAsync(It.IsAny<YoutubeDownloadHistory>()), Times.Exactly(3));
        fixture.HistoryRepoMock.Verify(r => r.SaveChangesAsync(), Times.Exactly(3));
    }

    [Fact]
    public async Task WritesHistory_UsingInjectedTimeProvider()
    {
        using var fixture = new Fixture(timeProvider: new FixedTimeProvider(new DateTimeOffset(2030, 5, 1, 12, 0, 0, TimeSpan.Zero)));
        var filePath = fixture.CreateReadyFile();
        fixture.YoutubeClientMock
            .Setup(c => c.DownloadVideoAsync(VideoId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new YoutubeClientResponse(filePath, true, []));
        var tempId = fixture.CreateJob();

        await RunToTerminalStatusAsync(fixture, tempId);

        fixture.HistoryRepoMock.Verify(
            r => r.AddAsync(It.Is<YoutubeDownloadHistory>(h =>
                h.DownloadedAt == new DateTime(2030, 5, 1, 12, 0, 0, DateTimeKind.Utc))),
            Times.Once);
    }

    [Fact]
    public async Task StartsSecondJob_WhileFirstDownloadIsStillInFlight()
    {
        using var fixture = new Fixture();
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstFile = fixture.CreateReadyFile();
        var secondFile = fixture.CreateReadyFile();

        fixture.YoutubeClientMock
            .Setup(c => c.DownloadVideoAsync(VideoId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, string _, CancellationToken _) =>
            {
                firstStarted.TrySetResult();
                await releaseFirst.Task;
                return new YoutubeClientResponse(firstFile, true, []);
            });
        fixture.YoutubeClientMock
            .Setup(c => c.DownloadVideoAsync("secondId", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new YoutubeClientResponse(secondFile, true, []))
            .Callback(() => secondStarted.TrySetResult());

        var firstTempId = fixture.CreateJob();
        var secondTempId = fixture.CreateJob("secondId");

        await fixture.Sut.StartAsync(CancellationToken.None);
        fixture.Queue.Writer.TryWrite(firstTempId);
        fixture.Queue.Writer.TryWrite(secondTempId);

        // The second download must begin while the first is still blocked in-flight.
        await firstStarted.Task.WaitAsync(PollTimeout);
        await secondStarted.Task.WaitAsync(PollTimeout);

        releaseFirst.TrySetResult();

        var firstStatus = await WaitForTerminalStatusAsync(fixture.JobStore, firstTempId);
        var secondStatus = await WaitForTerminalStatusAsync(fixture.JobStore, secondTempId);
        await fixture.Sut.StopAsync(CancellationToken.None);

        firstStatus.Status.Should().Be(DownloadJobStatus.Ready);
        secondStatus.Status.Should().Be(DownloadJobStatus.Ready);
    }

    [Fact]
    public async Task DrainsAllEnqueuedJobs_ToReady()
    {
        using var fixture = new Fixture();
        var filePath = fixture.CreateReadyFile();
        fixture.YoutubeClientMock
            .Setup(c => c.DownloadVideoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new YoutubeClientResponse(filePath, true, []));

        var tempIds = Enumerable.Range(0, 4).Select(_ => fixture.CreateJob()).ToArray();

        await fixture.Sut.StartAsync(CancellationToken.None);
        foreach (var tempId in tempIds)
            fixture.Queue.Writer.TryWrite(tempId);

        try
        {
            foreach (var tempId in tempIds)
            {
                var status = await WaitForTerminalStatusAsync(fixture.JobStore, tempId);
                status.Status.Should().Be(DownloadJobStatus.Ready);
            }
        }
        finally
        {
            await fixture.Sut.StopAsync(CancellationToken.None);
        }
    }
}
