using JiApp.YtDownloader.Domain;
using JiApp.YtDownloader.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JiApp.YtDownloader.Services;

public enum DownloadJobStatus
{
    Pending,
    Running,
    Ready,
    Failed
}

public sealed record DownloadJobInfo(
    long UserId,
    string VideoId,
    string VideoTitle,
    string? VideoDescription,
    string? VideoImageUrl,
    string VideoUrl);

public sealed record DownloadJobStatusResult(DownloadJobStatus Status, string? Error, string? ErrorCategory);

public interface IDownloadJobStore
{
    string CreateJob(long userId, string videoId, string videoTitle, string? videoDescription, string? videoImageUrl, string videoUrl);
    bool Claim(string tempId, long userId);
    void MarkReady(string tempId, long userId, string filePath);
    void MarkFailed(string tempId, long userId, string error, string? errorCategory = null);
    DownloadJobStatusResult? GetStatus(string tempId, long userId);
    DownloadJobInfo? GetJobInfo(string tempId);
    string? GetFilePath(string tempId, long userId);
    void CleanupExpired();
}

/// <summary>
/// Durable job store backed by the <see cref="DownloadCommand"/> table. The rows ARE the
/// work queue: the worker claims eligible rows, downloads, and marks them done or failed.
/// An exhausted Failed row is the dead-letter record — it stays visible to the user until
/// the TTL reaps it. Each operation opens a short-lived scoped <see cref="YtDbContext"/> so
/// the singleton store never captures a context.
/// </summary>
public sealed class DownloadJobStore : IDownloadJobStore, IDownloadQueue
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _ttl;
    private readonly TimeSpan _runningMaxAge;
    private readonly string? _baseDirectory;
    private readonly TimeProvider _timeProvider;

    public DownloadJobStore(
        IServiceScopeFactory scopeFactory,
        TimeSpan ttl,
        TimeProvider timeProvider,
        TimeSpan runningMaxAge,
        string? baseDirectory)
    {
        _scopeFactory = scopeFactory;
        _ttl = ttl;
        _timeProvider = timeProvider;
        _runningMaxAge = runningMaxAge;
        _baseDirectory = baseDirectory;
    }

    public string CreateJob(long userId, string videoId, string videoTitle, string? videoDescription, string? videoImageUrl, string videoUrl)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YtDbContext>();

        // Idempotency: a double-tap for the same video while the previous request is
        // still active/in-flight returns the same job instead of enqueueing a duplicate.
        // A Failed row awaiting its retry backoff (NextAttemptAt set) is still an active
        // job — it will be claimed again once the backoff elapses, so the re-tap dedupes
        // onto it instead of inserting a second row that would collide in the index.
        var activeId = db.DownloadCommands
            .Where(c => c.UserId == userId && c.VideoId == videoId
                && (c.Status == DownloadCommandStatus.Queued || c.Status == DownloadCommandStatus.Processing
                    || (c.Status == DownloadCommandStatus.Failed && c.NextAttemptAt != null)))
            .Select(c => c.Id)
            .FirstOrDefault();
        if (activeId is not null)
            return activeId;

        var command = DownloadCommand.Create(
            Guid.NewGuid().ToString("N"),
            userId,
            videoId,
            videoTitle,
            videoDescription,
            videoImageUrl,
            videoUrl,
            NowUtc(),
            _ttl);

        db.DownloadCommands.Add(command);
        try
        {
            db.SaveChanges();
        }
        catch (DbUpdateException)
        {
            // A concurrent request for the same active (UserId, VideoId) won the unique
            // filtered-index race — return its job instead of enqueueing a duplicate.
            // A Failed row awaiting its retry backoff counts as active here too.
            db.Entry(command).State = EntityState.Detached;
            var existingId = db.DownloadCommands
                .Where(c => c.UserId == userId && c.VideoId == videoId
                    && (c.Status == DownloadCommandStatus.Queued || c.Status == DownloadCommandStatus.Processing
                        || (c.Status == DownloadCommandStatus.Failed && c.NextAttemptAt != null)))
                .Select(c => c.Id)
                .FirstOrDefault();
            if (existingId is not null)
                return existingId;

            throw;
        }

        return command.Id;
    }

    /// <summary>
    /// Atomically claims a single freshly-Queued row for its owner, moving it to Processing.
    /// Returns true only for the caller that won the claim. This is the single-use primitive:
    /// it does NOT claim Failed retry rows — the worker's retry path uses
    /// <see cref="IDownloadQueue.ClaimEligible"/>, which also claims Failed rows whose
    /// backoff (<see cref="DownloadCommand.NextAttemptAt"/>) has elapsed.
    /// </summary>
    public bool Claim(string tempId, long userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YtDbContext>();

        var now = NowUtc();
        var affected = db.Database.ExecuteSqlRaw(
            """
            UPDATE "DownloadCommands" SET "Status" = {0}, "NextAttemptAt" = NULL, "ProcessingStartedAtUtc" = {4}
            WHERE "Id" = {1} AND "UserId" = {2} AND "Status" = {3}
            """,
            DownloadCommandStatus.Processing.ToString(),
            tempId,
            userId,
            DownloadCommandStatus.Queued.ToString(),
            now);

        return affected == 1;
    }

    public void MarkReady(string tempId, long userId, string filePath)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YtDbContext>();

        var command = db.DownloadCommands.FirstOrDefault(c => c.Id == tempId && c.UserId == userId);
        if (command is null)
            return;

        command.MarkReady(filePath, NowUtc(), _ttl);
        db.SaveChanges();
    }

    public void MarkFailed(string tempId, long userId, string error, string? errorCategory = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YtDbContext>();

        var command = db.DownloadCommands.FirstOrDefault(c => c.Id == tempId && c.UserId == userId);
        if (command is null)
            return;

        command.Fail(error, errorCategory, NowUtc());
        db.SaveChanges();
    }

    public DownloadJobStatusResult? GetStatus(string tempId, long userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YtDbContext>();

        var command = db.DownloadCommands
            .AsNoTracking()
            .Where(c => c.Id == tempId && c.UserId == userId)
            .Select(c => new { c.Status, c.NextAttemptAt, c.LastError, c.ErrorCategory })
            .FirstOrDefault();
        if (command is null)
            return null;

        var status = command.Status switch
        {
            DownloadCommandStatus.Queued => DownloadJobStatus.Pending,
            DownloadCommandStatus.Processing => DownloadJobStatus.Running,
            DownloadCommandStatus.Completed => DownloadJobStatus.Ready,
            // A Failed row still awaiting its retry backoff is an in-flight job. Reporting
            // it as Failed would make the mobile poller treat a scheduled retry as terminal
            // and throw while the worker is seconds away from retrying it.
            DownloadCommandStatus.Failed when command.NextAttemptAt is not null => DownloadJobStatus.Pending,
            DownloadCommandStatus.Failed => DownloadJobStatus.Failed,
            _ => DownloadJobStatus.Failed
        };

        return new DownloadJobStatusResult(status, command.LastError, command.ErrorCategory);
    }

    // Worker-only lookup keyed on the unguessable temp id; never exposed over HTTP.
    public DownloadJobInfo? GetJobInfo(string tempId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YtDbContext>();

        return db.DownloadCommands
            .AsNoTracking()
            .Where(c => c.Id == tempId)
            .Select(c => new DownloadJobInfo(c.UserId, c.VideoId, c.VideoTitle, c.VideoDescription, c.VideoImageUrl, c.VideoUrl))
            .FirstOrDefault();
    }

    public string? GetFilePath(string tempId, long userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YtDbContext>();

        var command = db.DownloadCommands
            .AsNoTracking()
            .Where(c => c.Id == tempId && c.UserId == userId)
            .Select(c => new { c.Status, c.FilePath, c.ExpiresAt })
            .FirstOrDefault();
        if (command is null)
            return null;

        if (command.Status != DownloadCommandStatus.Completed || command.FilePath is null)
            return null;

        if (NowUtc() > command.ExpiresAt || !File.Exists(command.FilePath))
            return null;

        return command.FilePath;
    }

    public void CleanupExpired()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YtDbContext>();

        var now = NowUtc();

        // The worker's per-job deadline normally fails a hung download. A job stuck in
        // Processing past runningMaxAge (deadline + grace) means the deadline path never
        // completed — force-expire it via the retry semantics so the existing backoff
        // machinery bounds the recovery, and delete its partial files so a retry starts clean.
        var runningCutoff = now.Add(-_runningMaxAge);
        var stuckProcessing = db.DownloadCommands
            .Where(c => c.Status == DownloadCommandStatus.Processing
                && c.ProcessingStartedAtUtc != null
                && c.ProcessingStartedAtUtc < runningCutoff)
            .ToList();

        foreach (var command in stuckProcessing)
        {
            command.Fail("Download timed out.", errorCategory: null, now);
            DeletePartialFilesForJob(command);
        }

        var expired = db.DownloadCommands
            .Where(c => c.ExpiresAt < now && c.Status != DownloadCommandStatus.Processing)
            .ToList();

        foreach (var command in expired)
        {
            // A completed download owns a file on disk — remove it with the row.
            if (command.Status == DownloadCommandStatus.Completed && command.FilePath is not null)
                TryDeleteFile(command.FilePath);

            db.DownloadCommands.Remove(command);
        }

        db.SaveChanges();
    }

    // Crash recovery: any row stuck in Processing was mid-download when the worker died.
    // Reset it to Queued so a fresh worker picks it up on startup. Resetting ALL Processing
    // rows assumes a single worker instance — with more than one, a live worker's in-flight
    // rows would need distinguishing via a host/lease column.
    public int ResetOrphanedProcessing()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YtDbContext>();

        return db.Database.ExecuteSqlRaw(
            "UPDATE \"DownloadCommands\" SET \"Status\" = {0}, \"ProcessingStartedAtUtc\" = NULL WHERE \"Status\" = {1}",
            DownloadCommandStatus.Queued.ToString(),
            DownloadCommandStatus.Processing.ToString());
    }

    /// <summary>
    /// The queue scan. Returns rows that are ready to run: freshly Queued, or Failed
    /// rows whose retry backoff (NextAttemptAt) has elapsed. Ordered oldest-first so a
    /// burst drains FIFO.
    /// </summary>
    public IReadOnlyList<string> GetEligibleTempIds(int maxCount)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YtDbContext>();

        var now = NowUtc();
        return db.DownloadCommands
            .AsNoTracking()
            .Where(c => c.Status == DownloadCommandStatus.Queued
                || (c.Status == DownloadCommandStatus.Failed && c.NextAttemptAt != null && c.NextAttemptAt <= now))
            .OrderBy(c => c.CreatedAtUtc)
            .Take(maxCount)
            .Select(c => c.Id)
            .ToList();
    }

    /// <summary>
    /// Atomically moves a single eligible row (Queued, or Failed with its backoff
    /// elapsed) to Processing. Returns true only for the worker that won the claim —
    /// a second worker touching the same row gets false.
    /// </summary>
    public bool ClaimEligible(string tempId, long userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YtDbContext>();

        var now = NowUtc();
        var affected = db.Database.ExecuteSqlRaw(
            """
            UPDATE "DownloadCommands" SET "Status" = {0}, "NextAttemptAt" = NULL, "ProcessingStartedAtUtc" = {6}
            WHERE "Id" = {1} AND "UserId" = {2}
              AND ("Status" = {3} OR ("Status" = {4} AND "NextAttemptAt" IS NOT NULL AND "NextAttemptAt" <= {5}))
            """,
            DownloadCommandStatus.Processing.ToString(),
            tempId,
            userId,
            DownloadCommandStatus.Queued.ToString(),
            DownloadCommandStatus.Failed.ToString(),
            now,
            now);

        return affected == 1;
    }

    private DateTime NowUtc() => _timeProvider.GetUtcNow().UtcDateTime;

    private void DeletePartialFilesForJob(DownloadCommand command)
    {
        var folder = YtDownloadFolders.ForUser(_baseDirectory, command.UserId);
        if (!Directory.Exists(folder))
            return;

        // Every file keyed to the temp id — the completed .mp3, an interrupted .part,
        // or a stream-separated .mp4/.webm/.m4a — is a partial left behind by the
        // hung run. The retry starts clean.
        foreach (var file in Directory.EnumerateFiles(folder, $"{command.Id}.*"))
            TryDeleteFile(file);
    }

    private static void TryDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            // File may be locked or protected — skip and continue cleanup
        }
    }
}
