using JiApp.YtDownloader.Services;

namespace JiApp.YtDownloader.Tests.Services;

public sealed class DownloadJobStoreTests
{
    private const long UserId = 1L;
    private const long OtherUserId = 2L;
    private const string VideoId = "dQw4w9WgXcQ";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);

    // ── CreateJob ──────────────────────────────────────────────────────────

    [Fact]
    public void CreateJob_ReturnsUniquePendingJob_ForUser()
    {
        using var fixture = new Fixture(Ttl, DateTimeOffset.UtcNow);

        var first = fixture.CreateJob();
        var second = fixture.CreateJob();

        first.Should().NotBe(second);
        first.Should().HaveLength(32);
        var status = fixture.Store.GetStatus(first, UserId);
        status.Should().NotBeNull();
        status!.Status.Should().Be(DownloadJobStatus.Pending);
    }

    // ── Claim ──────────────────────────────────────────────────────────────

    [Fact]
    public void Claim_TransitionsPendingToRunning_AndReturnsTrue()
    {
        using var fixture = new Fixture(Ttl, DateTimeOffset.UtcNow);
        var tempId = fixture.CreateJob();

        var claimed = fixture.Store.Claim(tempId, UserId);

        claimed.Should().BeTrue();
        fixture.Store.GetStatus(tempId, UserId)!.Status.Should().Be(DownloadJobStatus.Running);
    }

    [Fact]
    public void Claim_WhenAlreadyClaimed_ReturnsFalse()
    {
        using var fixture = new Fixture(Ttl, DateTimeOffset.UtcNow);
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);

        var secondClaim = fixture.Store.Claim(tempId, UserId);

        secondClaim.Should().BeFalse();
    }

    [Fact]
    public void Claim_WhenOwnedByAnotherUser_ReturnsFalse()
    {
        using var fixture = new Fixture(Ttl, DateTimeOffset.UtcNow);
        var tempId = fixture.CreateJob();

        var claimed = fixture.Store.Claim(tempId, OtherUserId);

        claimed.Should().BeFalse();
        fixture.Store.GetStatus(tempId, UserId)!.Status.Should().Be(DownloadJobStatus.Pending);
    }

    [Fact]
    public void Claim_WhenUnknownTempId_ReturnsFalse()
    {
        using var fixture = new Fixture(Ttl, DateTimeOffset.UtcNow);

        fixture.Store.Claim("does-not-exist", UserId).Should().BeFalse();
    }

    // ── MarkReady / MarkFailed ─────────────────────────────────────────────

    [Fact]
    public void MarkReady_TransitionsToReady_AndGetFilePathReturnsPath_WhenFileExists()
    {
        using var fixture = new Fixture(Ttl, DateTimeOffset.UtcNow);
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);
        var filePath = fixture.CreateFile();

        fixture.Store.MarkReady(tempId, UserId, filePath);

        fixture.Store.GetFilePath(tempId, UserId).Should().Be(filePath);
        fixture.Store.GetStatus(tempId, UserId)!.Status.Should().Be(DownloadJobStatus.Ready);
    }

    [Fact]
    public void MarkReady_ResetsExpiry_ToFreshTtl()
    {
        using var fixture = new Fixture(Ttl, new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);
        var filePath = fixture.CreateFile();
        fixture.Clock.Advance(TimeSpan.FromMinutes(14));

        fixture.Store.MarkReady(tempId, UserId, filePath);
        fixture.Clock.Advance(TimeSpan.FromMinutes(14));

        fixture.Store.GetFilePath(tempId, UserId).Should().Be(filePath);
    }

    [Fact]
    public void MarkFailed_TransitionsToFailed_WithErrorAndCategory()
    {
        using var fixture = new Fixture(Ttl, DateTimeOffset.UtcNow);
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);

        fixture.Store.MarkFailed(tempId, UserId, "Failed to download video.", "YoutubeDl");

        var status = fixture.Store.GetStatus(tempId, UserId);
        status!.Status.Should().Be(DownloadJobStatus.Failed);
        status.Error.Should().Be("Failed to download video.");
        status.ErrorCategory.Should().Be("YoutubeDl");
    }

    // ── GetStatus ownership ────────────────────────────────────────────────

    [Fact]
    public void GetStatus_ReturnsNull_ForUnknownTempId()
    {
        using var fixture = new Fixture(Ttl, DateTimeOffset.UtcNow);

        fixture.Store.GetStatus("does-not-exist", UserId).Should().BeNull();
    }

    [Fact]
    public void GetStatus_ReturnsNull_WhenOwnedByAnotherUser()
    {
        using var fixture = new Fixture(Ttl, DateTimeOffset.UtcNow);
        var tempId = fixture.CreateJob();

        fixture.Store.GetStatus(tempId, OtherUserId).Should().BeNull();
    }

    // ── GetFilePath guards ─────────────────────────────────────────────────

    [Fact]
    public void GetFilePath_ReturnsNull_WhenJobNotReady()
    {
        using var fixture = new Fixture(Ttl, DateTimeOffset.UtcNow);
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);

        fixture.Store.GetFilePath(tempId, UserId).Should().BeNull();
    }

    [Fact]
    public void GetFilePath_ReturnsNull_WhenFileMissing()
    {
        using var fixture = new Fixture(Ttl, DateTimeOffset.UtcNow);
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);
        fixture.Store.MarkReady(tempId, UserId, Path.Combine(fixture.TempDir, "missing.mp3"));

        fixture.Store.GetFilePath(tempId, UserId).Should().BeNull();
    }

    [Fact]
    public void GetFilePath_ReturnsNull_WhenExpired()
    {
        using var fixture = new Fixture(Ttl, new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);
        fixture.Store.MarkReady(tempId, UserId, fixture.CreateFile());

        fixture.Clock.Advance(Ttl.Add(TimeSpan.FromMinutes(1)));

        fixture.Store.GetFilePath(tempId, UserId).Should().BeNull();
    }

    [Fact]
    public void GetFilePath_ReturnsNull_WhenOwnedByAnotherUser()
    {
        using var fixture = new Fixture(Ttl, DateTimeOffset.UtcNow);
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);
        fixture.Store.MarkReady(tempId, UserId, fixture.CreateFile());

        fixture.Store.GetFilePath(tempId, OtherUserId).Should().BeNull();
    }

    // ── CleanupExpired ─────────────────────────────────────────────────────

    [Fact]
    public void CleanupExpired_RemovesExpiredReadyJob_AndDeletesFile()
    {
        using var fixture = new Fixture(Ttl, new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);
        var filePath = fixture.CreateFile();
        fixture.Store.MarkReady(tempId, UserId, filePath);
        fixture.Clock.Advance(Ttl.Add(TimeSpan.FromMinutes(1)));

        fixture.Store.CleanupExpired();

        fixture.Store.GetStatus(tempId, UserId).Should().BeNull();
        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public void CleanupExpired_RemovesExpiredPendingJob()
    {
        using var fixture = new Fixture(Ttl, new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var tempId = fixture.CreateJob();
        fixture.Clock.Advance(Ttl.Add(TimeSpan.FromMinutes(1)));

        fixture.Store.CleanupExpired();

        fixture.Store.GetStatus(tempId, UserId).Should().BeNull();
    }

    [Fact]
    public void CleanupExpired_RemovesExpiredFailedJob()
    {
        using var fixture = new Fixture(Ttl, new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);
        fixture.Store.MarkFailed(tempId, UserId, "boom");
        fixture.Clock.Advance(Ttl.Add(TimeSpan.FromMinutes(1)));

        fixture.Store.CleanupExpired();

        fixture.Store.GetStatus(tempId, UserId).Should().BeNull();
    }

    [Fact]
    public void CleanupExpired_KeepsRunningJob_EvenWhenExpired()
    {
        using var fixture = new Fixture(Ttl, new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);
        fixture.Clock.Advance(Ttl.Add(TimeSpan.FromMinutes(1)));

        fixture.Store.CleanupExpired();

        fixture.Store.GetStatus(tempId, UserId)!.Status.Should().Be(DownloadJobStatus.Running);
    }

    [Fact]
    public void CleanupExpired_KeepsLiveReadyJob()
    {
        using var fixture = new Fixture(Ttl, new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);
        fixture.Store.MarkReady(tempId, UserId, fixture.CreateFile());
        fixture.Clock.Advance(TimeSpan.FromMinutes(14));

        fixture.Store.CleanupExpired();

        fixture.Store.GetStatus(tempId, UserId)!.Status.Should().Be(DownloadJobStatus.Ready);
    }

    private sealed class Fixture : IDisposable
    {
        public DownloadJobStore Store { get; }
        public FakeTimeProvider Clock { get; }
        public string TempDir { get; }

        public Fixture(TimeSpan ttl, DateTimeOffset now)
        {
            Clock = new FakeTimeProvider(now);
            Store = new DownloadJobStore(ttl, Clock);
            TempDir = Directory.CreateTempSubdirectory("ytdl-store-tests-").FullName;
        }

        public string CreateJob(long userId = UserId) =>
            Store.CreateJob(userId, VideoId, "Title", "Description",
                "https://example.com/i.jpg", "https://youtube.com/watch?v=dQw4w9WgXcQ");

        public string CreateFile(string fileName = "song.mp3")
        {
            var path = Path.Combine(TempDir, fileName);
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

    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _now;

        public FakeTimeProvider(DateTimeOffset now) => _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan delta) => _now += delta;
    }
}
