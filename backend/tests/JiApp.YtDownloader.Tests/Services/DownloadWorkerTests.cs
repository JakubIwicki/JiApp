using System.Diagnostics;
using System.Threading.Channels;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.YtApi;
using JiApp.YtDownloader.Configuration;
using JiApp.YtDownloader.Domain;
using JiApp.YtDownloader.Persistence;
using JiApp.YtDownloader.Repositories;
using JiApp.YtDownloader.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public async Task MarksJobReady_AndWritesHistory_WhenDownloadSucceeds()
    {
        using var fixture = new Fixture();
        var filePath = fixture.CreateReadyFile();
        fixture.YoutubeClientMock
            .Setup(c => c.DownloadVideoAsync(VideoId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new YoutubeClientResponse(filePath, true, []));
        var tempId = fixture.CreateJob();

        var status = await RunToTerminalStatusAsync(fixture, tempId);

        status.Status.Should().Be(DownloadJobStatus.Ready);
        fixture.JobStore.GetFilePath(tempId, UserId).Should().Be(filePath);
        fixture.HistoryRepoMock.Verify(
            r => r.AddAsync(It.Is<YoutubeDownloadHistory>(h =>
                h.UserId == UserId && h.VideoId == VideoId && h.VideoTitle == "Title" &&
                h.VideoDescription == "Description" && h.VideoUrl == VideoUrl), It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.HistoryRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarksJobFailed_WithYoutubeDlCategory_WhenYtDlpFails()
    {
        using var fixture = new Fixture();
        fixture.YoutubeClientMock
            .Setup(c => c.DownloadVideoAsync(VideoId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new YoutubeClientResponse(null, false, ["Sign in to confirm you're not a bot"]));
        var tempId = fixture.CreateJob();

        var status = await RunToTerminalStatusAsync(fixture, tempId);

        status.Status.Should().Be(DownloadJobStatus.Failed);
        status.ErrorCategory.Should().Be(ResultCategories.YoutubeDl);
        status.Error.Should().NotBeNullOrWhiteSpace();
        fixture.HistoryRepoMock.Verify(r => r.AddAsync(It.IsAny<YoutubeDownloadHistory>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.HistoryRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarksJobFailed_WithSanitizedError_WhenYoutubeClientThrows()
    {
        using var fixture = new Fixture();
        fixture.YoutubeClientMock
            .Setup(c => c.DownloadVideoAsync(VideoId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("sensitive yt-dlp error details"));
        var tempId = fixture.CreateJob();

        var status = await RunToTerminalStatusAsync(fixture, tempId);

        status.Status.Should().Be(DownloadJobStatus.Failed);
        status.Error.Should().NotContain("sensitive yt-dlp error details");
        status.Error.Should().Contain("Failed to process download");
        fixture.HistoryRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarksJobFailed_WhenFileMissingDespiteSuccess()
    {
        using var fixture = new Fixture();
        fixture.YoutubeClientMock
            .Setup(c => c.DownloadVideoAsync(VideoId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new YoutubeClientResponse(null, true, []));
        var tempId = fixture.CreateJob();

        var status = await RunToTerminalStatusAsync(fixture, tempId);

        status.Status.Should().Be(DownloadJobStatus.Failed);
        status.ErrorCategory.Should().Be(ResultCategories.YoutubeDl);
        status.Error.Should().Contain("file is missing");
    }

    [Fact]
    public async Task MarksJobFailed_WhenFileIsEmpty()
    {
        using var fixture = new Fixture();
        var emptyFile = Path.Combine(fixture.TempDir, "empty.mp3");
        File.WriteAllBytes(emptyFile, []);
        fixture.YoutubeClientMock
            .Setup(c => c.DownloadVideoAsync(VideoId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new YoutubeClientResponse(emptyFile, true, []));
        var tempId = fixture.CreateJob();

        var status = await RunToTerminalStatusAsync(fixture, tempId);

        status.Status.Should().Be(DownloadJobStatus.Failed);
        status.ErrorCategory.Should().Be(ResultCategories.YoutubeDl);
        status.Error.Should().Contain("file is empty");
    }

    [Fact]
    public async Task MarksJobFailed_AsTimedOut_WhenDownloadExceedsTimeout()
    {
        using var fixture = new Fixture(downloadTimeout: TimeSpan.FromSeconds(1));
        fixture.YoutubeClientMock
            .Setup(c => c.DownloadVideoAsync(VideoId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, string _, string _, CancellationToken ct) =>
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
            .Setup(c => c.DownloadVideoAsync(VideoId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new YoutubeClientResponse(filePath, true, []));

        var attempts = 0;
        fixture.HistoryRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken _) =>
            {
                attempts++;
                if (attempts <= 2)
                    throw new InvalidOperationException("database unavailable");
            });
        var tempId = fixture.CreateJob();

        var status = await RunToTerminalStatusAsync(fixture, tempId);

        status.Status.Should().Be(DownloadJobStatus.Ready);
        fixture.HistoryRepoMock.Verify(r => r.AddAsync(It.IsAny<YoutubeDownloadHistory>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        fixture.HistoryRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task WritesHistory_UsingInjectedTimeProvider()
    {
        using var fixture = new Fixture(timeProvider: new FakeTimeProvider(new DateTimeOffset(2030, 5, 1, 12, 0, 0, TimeSpan.Zero)));
        var filePath = fixture.CreateReadyFile();
        fixture.YoutubeClientMock
            .Setup(c => c.DownloadVideoAsync(VideoId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new YoutubeClientResponse(filePath, true, []));
        var tempId = fixture.CreateJob();

        await RunToTerminalStatusAsync(fixture, tempId);

        fixture.HistoryRepoMock.Verify(
            r => r.AddAsync(It.Is<YoutubeDownloadHistory>(h =>
                h.DownloadedAt == new DateTime(2030, 5, 1, 12, 0, 0, DateTimeKind.Utc)), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PassesWorkerCancellationToken_ToHistoryRepository()
    {
        using var fixture = new Fixture();
        var filePath = fixture.CreateReadyFile();
        fixture.YoutubeClientMock
            .Setup(c => c.DownloadVideoAsync(VideoId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new YoutubeClientResponse(filePath, true, []));

        CancellationToken captured = default;
        fixture.HistoryRepoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(ct => captured = ct)
            .Returns(Task.CompletedTask);

        using var cts = new CancellationTokenSource();
        var tempId = fixture.CreateJob();

        await fixture.Sut.StartAsync(cts.Token);
        fixture.Queue.Writer.TryWrite(tempId);
        try
        {
            await WaitForTerminalStatusAsync(fixture.JobStore, tempId);
        }
        finally
        {
            await fixture.Sut.StopAsync(cts.Token);
        }

        // The worker's host-lifetime token must reach the history write, not be
        // dropped: before the fix SaveChangesAsync() passed CancellationToken.None
        // (CanBeCanceled == false), so a cancelable token proves it flowed through.
        captured.CanBeCanceled.Should().BeTrue();
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
            .Setup(c => c.DownloadVideoAsync(VideoId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, string _, string _, CancellationToken _) =>
            {
                firstStarted.TrySetResult();
                await releaseFirst.Task;
                return new YoutubeClientResponse(firstFile, true, []);
            });
        fixture.YoutubeClientMock
            .Setup(c => c.DownloadVideoAsync("secondId", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
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
            .Setup(c => c.DownloadVideoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
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

    [Fact]
    public async Task ResetsOrphanedProcessingJob_OnStartup_AndProcessesIt()
    {
        using var fixture = new Fixture();
        var filePath = fixture.CreateReadyFile();
        fixture.YoutubeClientMock
            .Setup(c => c.DownloadVideoAsync(VideoId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new YoutubeClientResponse(filePath, true, []));
        var tempId = fixture.CreateJob();
        fixture.JobStore.Claim(tempId, UserId);

        var status = await RunToTerminalStatusAsync(fixture, tempId);

        status.Status.Should().Be(DownloadJobStatus.Ready);
        fixture.HistoryRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RetriesFailedJob_AfterCooldown_AndSucceeds()
    {
        using var fixture = new Fixture(
            timeProvider: new FakeTimeProvider(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            pollInterval: TimeSpan.FromMilliseconds(50));
        var filePath = fixture.CreateReadyFile();
        var attempts = 0;
        fixture.YoutubeClientMock
            .Setup(c => c.DownloadVideoAsync(VideoId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, string _, string _, CancellationToken _) =>
            {
                attempts++;
                return attempts == 1
                    ? new YoutubeClientResponse(null, false, ["transient yt-dlp error"])
                    : new YoutubeClientResponse(filePath, true, []);
            });
        var tempId = fixture.CreateJob();

        await fixture.Sut.StartAsync(CancellationToken.None);
        fixture.Queue.Writer.TryWrite(tempId);
        try
        {
            await WaitForStatusAsync(fixture, tempId, DownloadJobStatus.Failed);

            fixture.Clock.Advance(TimeSpan.FromSeconds(31));

            var status = await WaitForStatusAsync(fixture, tempId, DownloadJobStatus.Ready);

            status.Status.Should().Be(DownloadJobStatus.Ready);
            attempts.Should().Be(2);
            fixture.HistoryRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            await fixture.Sut.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task ExhaustsRetries_AndNeverPicksTheJobUpAgain()
    {
        using var fixture = new Fixture(
            timeProvider: new FakeTimeProvider(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            pollInterval: TimeSpan.FromMilliseconds(50));
        fixture.YoutubeClientMock
            .Setup(c => c.DownloadVideoAsync(VideoId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new YoutubeClientResponse(null, false, ["permanent yt-dlp error"]));
        var tempId = fixture.CreateJob();

        await fixture.Sut.StartAsync(CancellationToken.None);
        fixture.Queue.Writer.TryWrite(tempId);
        try
        {
            await WaitForStatusAsync(fixture, tempId, DownloadJobStatus.Failed);
            fixture.Clock.Advance(TimeSpan.FromSeconds(31));
            await WaitForAttemptsRemainingAsync(fixture, tempId, 1);

            fixture.Clock.Advance(TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(1));
            await WaitForAttemptsRemainingAsync(fixture, tempId, 0);

            var row = fixture.LoadCommand(tempId);
            row.Should().NotBeNull();
            row!.Status.Should().Be(DownloadCommandStatus.Failed);
            row.AttemptsRemaining.Should().Be(0);
            row.NextAttemptAt.Should().BeNull();
            row.LastError.Should().NotBeNullOrWhiteSpace();

            fixture.Clock.Advance(TimeSpan.FromHours(1));
            await Task.Delay(300);

            var calls = fixture.YoutubeClientMock.Invocations.Count(i => i.Method.Name == nameof(IYoutubeClient.DownloadVideoAsync));
            calls.Should().Be(3);
            fixture.JobStore.GetStatus(tempId, UserId)!.Status.Should().Be(DownloadJobStatus.Failed);
        }
        finally
        {
            await fixture.Sut.StopAsync(CancellationToken.None);
        }
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

    private static async Task<DownloadJobStatusResult> WaitForStatusAsync(Fixture fixture, string tempId, DownloadJobStatus expected)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < PollTimeout)
        {
            var status = fixture.JobStore.GetStatus(tempId, UserId);
            if (status?.Status == expected)
                return status;

            await Task.Delay(20);
        }

        throw new TimeoutException($"Job {tempId} did not reach status {expected} within {PollTimeout}.");
    }

    private static async Task WaitForAttemptsRemainingAsync(Fixture fixture, string tempId, int expected)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < PollTimeout)
        {
            if (fixture.LoadCommand(tempId)?.AttemptsRemaining == expected)
                return;

            await Task.Delay(20);
        }

        throw new TimeoutException($"Job {tempId} did not reach {expected} attempts remaining within {PollTimeout}.");
    }

    private sealed class Fixture : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly DbContextOptions<YtDbContext> _options;

        public DownloadJobStore JobStore { get; }
        public Channel<string> Queue { get; } = Channel.CreateUnbounded<string>();
        public Mock<IYoutubeClient> YoutubeClientMock { get; } = new();
        public Mock<IDownloadHistoryRepository> HistoryRepoMock { get; } = new();
        public DownloadWorker Sut { get; }
        public string TempDir { get; }
        public FakeTimeProvider Clock { get; }

        public Fixture(TimeProvider? timeProvider = null, TimeSpan? downloadTimeout = null, TimeSpan? pollInterval = null)
        {
            Clock = (FakeTimeProvider?)timeProvider ?? new FakeTimeProvider(DateTimeOffset.UtcNow);
            TempDir = Directory.CreateTempSubdirectory("ytdl-worker-tests-").FullName;

            // The worker runs jobs concurrently, so each DbContext must own its own
            // connection (like production). A shared single :memory: connection serializes
            // badly across threads. A temp file with WAL + busy timeout mirrors production.
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(TempDir, "ytdl.db")
            }.ToString();
            _options = new DbContextOptionsBuilder<YtDbContext>()
                .UseSqlite(connectionString)
                .AddInterceptors(new SqliteBusyTimeoutInterceptor())
                .Options;
            using (var db = new YtDbContext(_options))
            {
                db.Database.Migrate();
                db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
            }

            var services = new ServiceCollection();
            services.AddScoped(_ => new YtDbContext(_options));
            services.AddScoped(_ => HistoryRepoMock.Object);
            _provider = services.BuildServiceProvider();

            JobStore = new DownloadJobStore(_provider.GetRequiredService<IServiceScopeFactory>(), TimeSpan.FromMinutes(15), Clock);
            var settings = new Settings { App = new Settings.AppSettings { BaseDirectory = TempDir } };

            Sut = new DownloadWorker(
                JobStore,
                JobStore,
                Queue,
                YoutubeClientMock.Object,
                _provider.GetRequiredService<IServiceScopeFactory>(),
                settings,
                NullLogger<DownloadWorker>.Instance,
                Clock,
                downloadTimeout,
                pollInterval);
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

        public DownloadCommand? LoadCommand(string tempId)
        {
            using var db = new YtDbContext(_options);
            return db.DownloadCommands.AsNoTracking().FirstOrDefault(c => c.Id == tempId);
        }

        public void Dispose()
        {
            _provider.Dispose();
            try
            {
                Directory.Delete(TempDir, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _now;

        public FakeTimeProvider(DateTimeOffset now) => _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan delta) => _now += delta;
    }
}
