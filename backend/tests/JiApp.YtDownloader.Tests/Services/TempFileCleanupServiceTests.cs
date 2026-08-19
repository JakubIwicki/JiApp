using System.Diagnostics;
using JiApp.Testing.Common.Fakes;
using JiApp.YtDownloader.Domain;
using JiApp.YtDownloader.Persistence;
using JiApp.YtDownloader.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.YtDownloader.Tests.Services;

public sealed class TempFileCleanupServiceTests
{
    private const long UserId = 1L;
    private const string VideoId = "dQw4w9WgXcQ";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);
    // Exceeds the TTL so a Processing row can be past ExpiresAt yet within the reaper
    // window — the state NeverReapsProcessingRow_EvenWhenExpired exercises. (Prod's reaper
    // is 10 min: 5-min deadline + 5-min grace, which reaps every Processing row before its TTL.)
    private static readonly TimeSpan RunningMaxAge = TimeSpan.FromMinutes(35);
    private static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(10);
    private static readonly DateTimeOffset FixedNow = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RemovesExpiredJob_AndItsFile()
    {
        using var fixture = new Fixture();
        var tempId = fixture.CreateReadyJob();
        var filePath = fixture.FilePathFor(tempId);
        fixture.Clock.Advance(Ttl.Add(TimeSpan.FromMinutes(1)));

        await RunOneSweepAsync(fixture, doneWhen: f => f.LoadCommand(tempId) is null);

        fixture.LoadCommand(tempId).Should().BeNull();
        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task KeepsLiveJob_AndItsFile()
    {
        using var fixture = new Fixture();
        var expiredId = fixture.CreateReadyJob("vid-expired");
        fixture.Clock.Advance(Ttl.Add(TimeSpan.FromMinutes(1)));
        var liveId = fixture.CreateReadyJob("vid-live");
        var liveFile = fixture.FilePathFor(liveId);
        var expiredFile = fixture.FilePathFor(expiredId);

        await RunOneSweepAsync(fixture, doneWhen: f => f.LoadCommand(expiredId) is null);

        fixture.LoadCommand(liveId).Should().NotBeNull();
        File.Exists(liveFile).Should().BeTrue();
        File.Exists(expiredFile).Should().BeFalse();
    }

    [Fact]
    public async Task NeverReapsProcessingRow_EvenWhenExpired()
    {
        using var fixture = new Fixture();
        var processingId = fixture.CreateProcessingJob("vid-processing");
        var signalId = fixture.CreateReadyJob("vid-signal");
        fixture.Clock.Advance(Ttl.Add(TimeSpan.FromMinutes(1)));

        await RunOneSweepAsync(fixture, doneWhen: f => f.LoadCommand(signalId) is null);

        fixture.LoadCommand(processingId).Should().NotBeNull();
        fixture.LoadCommand(processingId)!.Status.Should().Be(DownloadCommandStatus.Processing);
    }

    [Fact]
    public async Task DoesNotThrow_WhenFileMissing()
    {
        using var fixture = new Fixture();
        var tempId = fixture.CreateReadyJob();
        var filePath = fixture.FilePathFor(tempId);
        File.Delete(filePath);
        fixture.Clock.Advance(Ttl.Add(TimeSpan.FromMinutes(1)));

        var act = () => RunOneSweepAsync(fixture, doneWhen: f => f.LoadCommand(tempId) is null);

        await act.Should().NotThrowAsync();
        fixture.LoadCommand(tempId).Should().BeNull();
    }

    [Fact]
    public async Task StopsLoop_OnCancellation()
    {
        using var fixture = new Fixture();
        var expiredId = fixture.CreateReadyJob("vid-expired");
        fixture.Clock.Advance(Ttl.Add(TimeSpan.FromMinutes(1)));
        var liveId = fixture.CreateReadyJob("vid-live");
        var liveFile = fixture.FilePathFor(liveId);

        await fixture.Sut.StartAsync(CancellationToken.None);
        try
        {
            await WaitUntilAsync(fixture, f => f.LoadCommand(expiredId) is null);
        }
        finally
        {
            // StopAsync cancels the loop's internal token; a sweep that ignored
            // cancellation would keep sleeping on its 5-minute delay and blow the timeout.
            using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await fixture.Sut.StopAsync(stopTimeout.Token);
        }

        fixture.LoadCommand(liveId).Should().NotBeNull();
        File.Exists(liveFile).Should().BeTrue();
    }

    private static async Task RunOneSweepAsync(Fixture fixture, Func<Fixture, bool> doneWhen)
    {
        await fixture.Sut.StartAsync(CancellationToken.None);
        try
        {
            await WaitUntilAsync(fixture, doneWhen);
        }
        finally
        {
            using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await fixture.Sut.StopAsync(stopTimeout.Token);
        }
    }

    private static async Task WaitUntilAsync(Fixture fixture, Func<Fixture, bool> done)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < PollTimeout)
        {
            if (done(fixture))
                return;

            await Task.Delay(20);
        }

        throw new TimeoutException($"Cleanup effect not observed within {PollTimeout}.");
    }

    private sealed class Fixture : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly DbContextOptions<YtDbContext> _options;

        public FakeTimeProvider Clock { get; }
        public DownloadJobStore JobStore { get; }
        public TempFileCleanupService Sut { get; }
        public string TempDir { get; }

        public Fixture()
        {
            Clock = new FakeTimeProvider(FixedNow);
            TempDir = Directory.CreateTempSubdirectory("ytdl-cleanup-tests-").FullName;

            // The service sweeps on its own thread while the test polls rows, so each
            // context must own its own connection (like production). A temp file with WAL
            // + busy timeout mirrors production and avoids "database is locked".
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
            _provider = services.BuildServiceProvider();

            JobStore = new DownloadJobStore(
                _provider.GetRequiredService<IServiceScopeFactory>(), Ttl, Clock, RunningMaxAge, TempDir);
            Sut = new TempFileCleanupService(JobStore, Mock.Of<ILogger<TempFileCleanupService>>());
        }

        public string CreateReadyJob(string videoId = VideoId)
        {
            var tempId = JobStore.CreateJob(
                UserId, videoId, "Title", null, null, $"https://youtube.com/watch?v={videoId}");
            JobStore.Claim(tempId, UserId);
            var filePath = Path.Combine(TempDir, $"{Guid.NewGuid():N}.mp3");
            File.WriteAllBytes(filePath, [0x49, 0x44, 0x33]);
            JobStore.MarkReady(tempId, UserId, filePath);
            return tempId;
        }

        public string CreateProcessingJob(string videoId = VideoId)
        {
            var tempId = JobStore.CreateJob(
                UserId, videoId, "Title", null, null, $"https://youtube.com/watch?v={videoId}");
            JobStore.Claim(tempId, UserId);
            return tempId;
        }

        public string FilePathFor(string tempId) => LoadCommand(tempId)!.FilePath!;

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
}
