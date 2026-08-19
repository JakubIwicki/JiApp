using JiApp.Common.Abstractions;
using JiApp.YtDownloader.Domain;
using JiApp.YtDownloader.Persistence;
using JiApp.YtDownloader.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JiApp.YtDownloader.Tests.Services;

public sealed class DownloadJobStoreTests
{
    private const long UserId = 1L;
    private const long OtherUserId = 2L;
    private const string VideoId = "dQw4w9WgXcQ";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);
    // Exceeds the TTL so a Processing row can be past ExpiresAt yet within the reaper
    // window — the state the CleanupExpired tests exercise. (Prod's reaper is 10 min:
    // 5-min deadline + 5-min grace, which reaps every Processing row before its TTL.)
    private static readonly TimeSpan RunningMaxAge = TimeSpan.FromMinutes(35);
    private static readonly DateTimeOffset FixedNow = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // ── CreateJob ──────────────────────────────────────────────────────────

    [Fact]
    public void CreateJob_ReturnsUniquePendingJob_ForUser()
    {
        using var fixture = new Fixture(Ttl, DateTimeOffset.UtcNow);

        var first = fixture.CreateJob(videoId: "video-one");
        var second = fixture.CreateJob(videoId: "video-two");

        first.Should().NotBe(second);
        first.Should().HaveLength(32);
        var status = fixture.Store.GetStatus(first, UserId);
        status.Should().NotBeNull();
        status!.Status.Should().Be(DownloadJobStatus.Pending);
    }

    [Fact]
    public void CreateJob_WhileActiveJobExists_ReturnsSameTempId_ForSameVideo()
    {
        using var fixture = new Fixture(Ttl, DateTimeOffset.UtcNow);

        var first = fixture.CreateJob();
        var second = fixture.CreateJob();

        second.Should().Be(first);
    }

    [Fact]
    public void CreateJob_AfterJobCompletes_ReturnsNewTempId_ForSameVideo()
    {
        using var fixture = new Fixture(Ttl, DateTimeOffset.UtcNow);
        var first = fixture.CreateJob();
        fixture.Store.Claim(first, UserId);
        fixture.Store.MarkReady(first, UserId, fixture.CreateFile());

        var second = fixture.CreateJob();

        second.Should().NotBe(first);
    }

    [Fact]
    public void CreateJob_WhileRetryScheduled_ReturnsSameTempId_AndKeepsOneRow()
    {
        using var fixture = new Fixture(Ttl, FixedNow);
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);
        fixture.Store.MarkFailed(tempId, UserId, "transient error", ResultCategories.YoutubeDl);
        fixture.LoadCommand(tempId)!.NextAttemptAt.Should().NotBeNull();

        var retapId = fixture.CreateJob();

        retapId.Should().Be(tempId);
        fixture.RowCount(UserId, VideoId).Should().Be(1);
    }

    [Fact]
    public void CreateJob_WhenRetriesExhausted_ReturnsNewTempId_AndKeepsDeadLetterRow()
    {
        using var fixture = new Fixture(Ttl, FixedNow);
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);
        fixture.Store.MarkFailed(tempId, UserId, "error one", ResultCategories.YoutubeDl);
        fixture.Clock.Advance(TimeSpan.FromSeconds(31));
        fixture.Store.MarkFailed(tempId, UserId, "error two", ResultCategories.YoutubeDl);
        fixture.Clock.Advance(TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(1));
        fixture.Store.MarkFailed(tempId, UserId, "error three", ResultCategories.YoutubeDl);
        fixture.LoadCommand(tempId)!.NextAttemptAt.Should().BeNull();

        var retapId = fixture.CreateJob();

        retapId.Should().NotBe(tempId);
        fixture.RowCount(UserId, VideoId).Should().Be(2);
    }

    [Fact]
    public void CreateJob_ForDifferentUser_SameVideo_ReturnsDifferentTempId()
    {
        using var fixture = new Fixture(Ttl, DateTimeOffset.UtcNow);

        var first = fixture.CreateJob(userId: UserId);
        var second = fixture.CreateJob(userId: OtherUserId);

        second.Should().NotBe(first);
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

    [Fact]
    public void Claim_StampsProcessingStartedAt()
    {
        using var fixture = new Fixture(Ttl, FixedNow);
        var tempId = fixture.CreateJob();

        fixture.Store.Claim(tempId, UserId);

        fixture.LoadCommand(tempId)!.ProcessingStartedAtUtc.Should().Be(FixedNow.UtcDateTime);
    }

    [Fact]
    public void ClaimEligible_StampsProcessingStartedAt()
    {
        using var fixture = new Fixture(Ttl, FixedNow);
        var tempId = fixture.CreateJob();

        ((IDownloadQueue)fixture.Store).ClaimEligible(tempId, UserId).Should().BeTrue();

        fixture.LoadCommand(tempId)!.ProcessingStartedAtUtc.Should().Be(FixedNow.UtcDateTime);
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
        using var fixture = new Fixture(Ttl, FixedNow);
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);
        var filePath = fixture.CreateFile();
        fixture.Clock.Advance(TimeSpan.FromMinutes(14));

        fixture.Store.MarkReady(tempId, UserId, filePath);
        fixture.Clock.Advance(TimeSpan.FromMinutes(14));

        fixture.Store.GetFilePath(tempId, UserId).Should().Be(filePath);
    }

    [Fact]
    public void MarkReady_ClearsProcessingStartedAt()
    {
        using var fixture = new Fixture(Ttl, FixedNow);
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);

        fixture.Store.MarkReady(tempId, UserId, fixture.CreateFile());

        fixture.LoadCommand(tempId)!.ProcessingStartedAtUtc.Should().BeNull();
    }

    [Fact]
    public void MarkFailed_TransitionsToFailed_WithErrorAndCategory_AndReportsPending_WhileRetryScheduled()
    {
        using var fixture = new Fixture(Ttl, DateTimeOffset.UtcNow);
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);

        fixture.Store.MarkFailed(tempId, UserId, "Failed to download video.", ResultCategories.YoutubeDl);

        var row = fixture.LoadCommand(tempId);
        row!.Status.Should().Be(DownloadCommandStatus.Failed);
        row.LastError.Should().Be("Failed to download video.");
        row.ErrorCategory.Should().Be(ResultCategories.YoutubeDl);

        // A retry is scheduled, so the job is still in-flight — it must not report failed
        // to the poller while the worker is seconds away from retrying it.
        var status = fixture.Store.GetStatus(tempId, UserId);
        status!.Status.Should().Be(DownloadJobStatus.Pending);
        status.Error.Should().Be("Failed to download video.");
        status.ErrorCategory.Should().Be(ResultCategories.YoutubeDl);
    }

    // ── Retry / DLQ ────────────────────────────────────────────────────────

    [Fact]
    public void MarkFailed_WithAttemptsRemaining_SchedulesRetry_AndBecomesEligibleAfterBackoff()
    {
        using var fixture = new Fixture(Ttl, FixedNow);
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);

        fixture.Store.MarkFailed(tempId, UserId, "transient error", ResultCategories.YoutubeDl);

        var row = fixture.LoadCommand(tempId);
        row!.Status.Should().Be(DownloadCommandStatus.Failed);
        row.AttemptsRemaining.Should().Be(2);
        row.LastError.Should().Be("transient error");
        row.NextAttemptAt.Should().Be(new DateTime(2030, 1, 1, 0, 0, 30, DateTimeKind.Utc));
        fixture.IsEligible(tempId).Should().BeFalse();

        fixture.Clock.Advance(TimeSpan.FromSeconds(31));

        fixture.IsEligible(tempId).Should().BeTrue();
    }

    [Fact]
    public void MarkFailed_AfterThreeAttempts_ExhaustsRetries_AndNeverScheduledAgain()
    {
        using var fixture = new Fixture(Ttl, FixedNow);
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);

        fixture.Store.MarkFailed(tempId, UserId, "error one", ResultCategories.YoutubeDl);
        fixture.Clock.Advance(TimeSpan.FromSeconds(31));
        fixture.Store.MarkFailed(tempId, UserId, "error two", ResultCategories.YoutubeDl);
        fixture.Clock.Advance(TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(1));
        fixture.Store.MarkFailed(tempId, UserId, "error three", ResultCategories.YoutubeDl);

        var row = fixture.LoadCommand(tempId);
        row!.Status.Should().Be(DownloadCommandStatus.Failed);
        row.AttemptsRemaining.Should().Be(0);
        row.NextAttemptAt.Should().BeNull();
        row.LastError.Should().Be("error three");

        fixture.Clock.Advance(TimeSpan.FromHours(1));

        fixture.IsEligible(tempId).Should().BeFalse();
    }

    // ── Crash recovery ─────────────────────────────────────────────────────

    [Fact]
    public void ResetOrphanedProcessing_ReturnsProcessingJob_ToQueued()
    {
        using var fixture = new Fixture(Ttl, DateTimeOffset.UtcNow);
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);
        fixture.Store.GetStatus(tempId, UserId)!.Status.Should().Be(DownloadJobStatus.Running);

        fixture.Store.ResetOrphanedProcessing().Should().Be(1);

        fixture.LoadCommand(tempId)!.Status.Should().Be(DownloadCommandStatus.Queued);
        fixture.Store.GetStatus(tempId, UserId)!.Status.Should().Be(DownloadJobStatus.Pending);
    }

    // ── Idempotency race (DB-enforced) ─────────────────────────────────────

    [Fact]
    public void UniqueFilteredIndex_RejectsDuplicateActiveRow_ForSameUserAndVideo()
    {
        using var fixture = new Fixture(Ttl, DateTimeOffset.UtcNow);
        fixture.SeedActiveRow(UserId, VideoId);

        var act = () => fixture.SeedActiveRow(UserId, VideoId);

        act.Should().Throw<DbUpdateException>();
    }

    [Fact]
    public void UniqueFilteredIndex_AllowsSameVideo_WhenPreviousRowIsCompleted()
    {
        using var fixture = new Fixture(Ttl, DateTimeOffset.UtcNow);
        fixture.SeedCompletedRow(UserId, VideoId);

        var act = () => fixture.SeedActiveRow(UserId, VideoId);

        act.Should().NotThrow();
    }

    [Fact]
    public void UniqueFilteredIndex_RejectsSecondActiveRow_WhileRetryScheduled()
    {
        using var fixture = new Fixture(Ttl, FixedNow);
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);
        fixture.Store.MarkFailed(tempId, UserId, "transient error", ResultCategories.YoutubeDl);
        fixture.LoadCommand(tempId)!.NextAttemptAt.Should().NotBeNull();

        var act = () => fixture.SeedActiveRow(UserId, VideoId);

        act.Should().Throw<DbUpdateException>();
    }

    [Fact]
    public async Task CreateJob_ConcurrentRequests_ForSameVideo_ReturnSameTempId_AndInsertOneRow()
    {
        using var fixture = new ConcurrencyFixture();

        var tasks = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(() => fixture.Store.CreateJob(
                UserId, VideoId, "Title", null, null, "https://youtube.com/watch?v=dQw4w9WgXcQ")))
            .ToArray();
        var tempIds = await Task.WhenAll(tasks);

        tempIds.Should().OnlyContain(id => id == tempIds[0]);
        tempIds[0].Should().HaveLength(32);
        fixture.ActiveRowCount(UserId, VideoId).Should().Be(1);
    }

    // ── Empty title is valid (optional metadata) ───────────────────────────

    [Fact]
    public void CreateJob_WithEmptyTitle_DoesNotThrow()
    {
        using var fixture = new Fixture(Ttl, DateTimeOffset.UtcNow);

        var act = () => fixture.Store.CreateJob(
            UserId, VideoId, string.Empty, null, null, "https://youtube.com/watch?v=dQw4w9WgXcQ");

        act.Should().NotThrow();
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

    [Fact]
    public void GetStatus_ReportsPending_WhileRetryScheduled()
    {
        using var fixture = new Fixture(Ttl, FixedNow);
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);
        fixture.Store.MarkFailed(tempId, UserId, "transient error", ResultCategories.YoutubeDl);

        var status = fixture.Store.GetStatus(tempId, UserId);

        status!.Status.Should().Be(DownloadJobStatus.Pending);
        status.Error.Should().Be("transient error");
        status.ErrorCategory.Should().Be(ResultCategories.YoutubeDl);
    }

    [Fact]
    public void GetStatus_ReportsFailed_WhenRetriesExhausted()
    {
        using var fixture = new Fixture(Ttl, FixedNow);
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);
        fixture.Store.MarkFailed(tempId, UserId, "error one");
        fixture.Clock.Advance(TimeSpan.FromSeconds(31));
        fixture.Store.MarkFailed(tempId, UserId, "error two");
        fixture.Clock.Advance(TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(1));
        fixture.Store.MarkFailed(tempId, UserId, "error three");

        var status = fixture.Store.GetStatus(tempId, UserId);

        status!.Status.Should().Be(DownloadJobStatus.Failed);
        status.Error.Should().Be("error three");
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
        using var fixture = new Fixture(Ttl, FixedNow);
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
        using var fixture = new Fixture(Ttl, FixedNow);
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
        using var fixture = new Fixture(Ttl, FixedNow);
        var tempId = fixture.CreateJob();
        fixture.Clock.Advance(Ttl.Add(TimeSpan.FromMinutes(1)));

        fixture.Store.CleanupExpired();

        fixture.Store.GetStatus(tempId, UserId).Should().BeNull();
    }

    [Fact]
    public void CleanupExpired_RemovesExpiredFailedJob()
    {
        using var fixture = new Fixture(Ttl, FixedNow);
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
        using var fixture = new Fixture(Ttl, FixedNow);
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);
        fixture.Clock.Advance(Ttl.Add(TimeSpan.FromMinutes(1)));

        fixture.Store.CleanupExpired();

        fixture.Store.GetStatus(tempId, UserId)!.Status.Should().Be(DownloadJobStatus.Running);
    }

    [Fact]
    public void CleanupExpired_ReapsStuckProcessingJob_WhenPastRunningMaxAge()
    {
        using var fixture = new Fixture(Ttl, FixedNow);
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);
        var folder = YtDownloadFolders.ForUser(fixture.TempDir, UserId);
        Directory.CreateDirectory(folder);
        var partialPath = Path.Combine(folder, $"{tempId}.mp3.part");
        File.WriteAllText(partialPath, "partial download");
        fixture.Clock.Advance(RunningMaxAge.Add(TimeSpan.FromMinutes(1)));

        fixture.Store.CleanupExpired();

        var row = fixture.LoadCommand(tempId);
        row!.Status.Should().Be(DownloadCommandStatus.Failed);
        row.LastError.Should().Be("Download timed out.");
        row.ProcessingStartedAtUtc.Should().BeNull();
        File.Exists(partialPath).Should().BeFalse();
    }

    [Fact]
    public void CleanupExpired_ReapsStuckProcessingJob_AndDeletesMp4AndWebmPartials()
    {
        using var fixture = new Fixture(Ttl, FixedNow);
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);
        var folder = YtDownloadFolders.ForUser(fixture.TempDir, UserId);
        Directory.CreateDirectory(folder);
        var mp4Partial = Path.Combine(folder, $"{tempId}.f137.mp4");
        var webmPartial = Path.Combine(folder, $"{tempId}.f251.webm");
        File.WriteAllText(mp4Partial, "x");
        File.WriteAllText(webmPartial, "x");
        fixture.Clock.Advance(RunningMaxAge.Add(TimeSpan.FromMinutes(1)));

        fixture.Store.CleanupExpired();

        File.Exists(mp4Partial).Should().BeFalse();
        File.Exists(webmPartial).Should().BeFalse();
    }

    [Fact]
    public void CleanupExpired_KeepsProcessingJob_WithinRunningMaxAge()
    {
        using var fixture = new Fixture(Ttl, FixedNow);
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);
        fixture.Clock.Advance(RunningMaxAge.Subtract(TimeSpan.FromMinutes(1)));

        fixture.Store.CleanupExpired();

        fixture.LoadCommand(tempId)!.Status.Should().Be(DownloadCommandStatus.Processing);
    }

    [Fact]
    public void CleanupExpired_KeepsLiveReadyJob()
    {
        using var fixture = new Fixture(Ttl, FixedNow);
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);
        fixture.Store.MarkReady(tempId, UserId, fixture.CreateFile());
        fixture.Clock.Advance(TimeSpan.FromMinutes(14));

        fixture.Store.CleanupExpired();

        fixture.Store.GetStatus(tempId, UserId)!.Status.Should().Be(DownloadJobStatus.Ready);
    }

    [Fact]
    public void CleanupExpired_ReapsProcessingRow_WithNullStartedAt_WhenCreatedAtIsStale()
    {
        using var fixture = new Fixture(Ttl, FixedNow);
        var tempId = fixture.SeedProcessingRow(FixedNow.UtcDateTime.AddMinutes(-(int)RunningMaxAge.TotalMinutes - 1));
        var folder = YtDownloadFolders.ForUser(fixture.TempDir, UserId);
        Directory.CreateDirectory(folder);
        var partialPath = Path.Combine(folder, $"{tempId}.mp3.part");
        File.WriteAllText(partialPath, "partial download");

        fixture.Store.CleanupExpired();

        var row = fixture.LoadCommand(tempId);
        row!.Status.Should().Be(DownloadCommandStatus.Failed);
        row.LastError.Should().Be("Download timed out.");
        File.Exists(partialPath).Should().BeFalse();
    }

    [Fact]
    public void CleanupExpired_KeepsProcessingRow_WithNullStartedAt_WhenCreatedAtIsRecent()
    {
        using var fixture = new Fixture(Ttl, FixedNow);
        var tempId = fixture.SeedProcessingRow(FixedNow.UtcDateTime);
        var folder = YtDownloadFolders.ForUser(fixture.TempDir, UserId);
        Directory.CreateDirectory(folder);
        var partialPath = Path.Combine(folder, $"{tempId}.mp3.part");
        File.WriteAllText(partialPath, "partial download");

        fixture.Store.CleanupExpired();

        var row = fixture.LoadCommand(tempId);
        row!.Status.Should().Be(DownloadCommandStatus.Processing);
        row.ProcessingStartedAtUtc.Should().BeNull();
        File.Exists(partialPath).Should().BeTrue();
    }

    [Fact]
    public void CleanupExpired_KeepsFailedRow_WithFutureNextAttemptAt_EvenWhenExpired()
    {
        using var fixture = new Fixture(Ttl, FixedNow);
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);
        fixture.Clock.Advance(Ttl.Add(TimeSpan.FromMinutes(1)));
        fixture.Store.MarkFailed(tempId, UserId, "transient error", ResultCategories.YoutubeDl);

        fixture.Store.CleanupExpired();

        // A Failed row mid-backoff is still an in-flight job — the TTL sweep must not
        // delete it, or the remaining retries die and the client poll 404s.
        var row = fixture.LoadCommand(tempId);
        row.Should().NotBeNull();
        row!.Status.Should().Be(DownloadCommandStatus.Failed);
        row.NextAttemptAt.Should().NotBeNull();
    }

    [Fact]
    public void CleanupExpired_RemovesExhaustedFailedJob_WhenExpired()
    {
        using var fixture = new Fixture(Ttl, FixedNow);
        var tempId = fixture.CreateJob();
        fixture.Store.Claim(tempId, UserId);
        fixture.Store.MarkFailed(tempId, UserId, "error one");
        fixture.Clock.Advance(TimeSpan.FromSeconds(31));
        fixture.Store.MarkFailed(tempId, UserId, "error two");
        fixture.Clock.Advance(TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(1));
        fixture.Store.MarkFailed(tempId, UserId, "error three");
        fixture.Clock.Advance(Ttl.Add(TimeSpan.FromMinutes(1)));

        fixture.Store.CleanupExpired();

        // A dead-letter row (NextAttemptAt null) has no retry left to protect — the TTL reaps it.
        fixture.Store.GetStatus(tempId, UserId).Should().BeNull();
    }

    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _provider;
        private readonly DbContextOptions<YtDbContext> _options;

        public DownloadJobStore Store { get; }
        public FakeTimeProvider Clock { get; }
        public string TempDir { get; }

        public Fixture(TimeSpan ttl, DateTimeOffset now)
        {
            Clock = new FakeTimeProvider(now);
            TempDir = Directory.CreateTempSubdirectory("ytdl-store-tests-").FullName;
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            _options = new DbContextOptionsBuilder<YtDbContext>()
                .UseSqlite(_connection)
                .Options;
            using (var db = new YtDbContext(_options))
                db.Database.Migrate();

            var services = new ServiceCollection();
            services.AddScoped(_ => new YtDbContext(_options));
            _provider = services.BuildServiceProvider();

            Store = new DownloadJobStore(
                _provider.GetRequiredService<IServiceScopeFactory>(), ttl, Clock, RunningMaxAge, TempDir);
        }

        public string CreateJob(long userId = UserId, string videoId = VideoId) =>
            Store.CreateJob(userId, videoId, "Title", "Description",
                "https://example.com/i.jpg", "https://youtube.com/watch?v=dQw4w9WgXcQ");

        public string CreateFile(string fileName = "song.mp3")
        {
            var path = Path.Combine(TempDir, fileName);
            File.WriteAllBytes(path, [0x49, 0x44, 0x33]);
            return path;
        }

        public DownloadCommand? LoadCommand(string tempId)
        {
            using var db = new YtDbContext(_options);
            return db.DownloadCommands.AsNoTracking().FirstOrDefault(c => c.Id == tempId);
        }

        public bool IsEligible(string tempId) => Store.GetEligibleTempIds(100).Contains(tempId);

        public int RowCount(long userId, string videoId)
        {
            using var db = new YtDbContext(_options);
            return db.DownloadCommands.Count(c => c.UserId == userId && c.VideoId == videoId);
        }

        // Bypasses the store's dedupe to exercise the unique filtered index directly.
        public void SeedActiveRow(long userId, string videoId)
        {
            using var db = new YtDbContext(_options);
            db.DownloadCommands.Add(NewCommand(userId, videoId));
            db.SaveChanges();
        }

        public void SeedCompletedRow(long userId, string videoId)
        {
            using var db = new YtDbContext(_options);
            var command = NewCommand(userId, videoId);
            command.MarkReady(Path.Combine(TempDir, "completed.mp3"), FixedNow.UtcDateTime, Ttl);
            db.DownloadCommands.Add(command);
            db.SaveChanges();
        }

        // Bypasses Claim (which always stamps ProcessingStartedAtUtc) to construct the
        // pre-migration/anomalous shape: a Processing row with no start timestamp.
        public string SeedProcessingRow(DateTime createdAtUtc)
        {
            using var db = new YtDbContext(_options);
            var command = NewCommand(UserId, VideoId);
            db.Entry(command).Property(c => c.CreatedAtUtc).CurrentValue = createdAtUtc;
            db.Entry(command).Property(c => c.Status).CurrentValue = DownloadCommandStatus.Processing;
            db.DownloadCommands.Add(command);
            db.SaveChanges();
            return command.Id;
        }

        private static DownloadCommand NewCommand(long userId, string videoId) =>
            DownloadCommand.Create(
                Guid.NewGuid().ToString("N"), userId, videoId, "Title", null, null,
                "https://youtube.com/watch?v=dQw4w9WgXcQ",
                FixedNow.UtcDateTime, Ttl);

        public void Dispose()
        {
            _provider.Dispose();
            _connection.Dispose();
            try
            {
                Directory.Delete(TempDir, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    // The concurrency test must run on a temp-file DB with WAL + busy timeout so that each
    // concurrent CreateJob opens its OWN connection (a shared in-memory connection serializes
    // and never exercises the insert race; without busy timeout, a second writer would see
    // "database is locked" instead of the unique constraint).
    private sealed class ConcurrencyFixture : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly DbContextOptions<YtDbContext> _options;
        private readonly string _tempDir;

        public DownloadJobStore Store { get; }

        public ConcurrencyFixture()
        {
            _tempDir = Directory.CreateTempSubdirectory("ytdl-race-tests-").FullName;
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(_tempDir, "ytdl.db")
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

            Store = new DownloadJobStore(
                _provider.GetRequiredService<IServiceScopeFactory>(),
                Ttl,
                new FakeTimeProvider(DateTimeOffset.UtcNow),
                RunningMaxAge,
                _tempDir);
        }

        public int ActiveRowCount(long userId, string videoId)
        {
            using var db = new YtDbContext(_options);
            return db.DownloadCommands.Count(c => c.UserId == userId && c.VideoId == videoId);
        }

        public void Dispose()
        {
            _provider.Dispose();
            try
            {
                Directory.Delete(_tempDir, recursive: true);
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
