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
public sealed class DownloadJobStore : IDownloadJobStore
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _ttl;
    private readonly TimeProvider _timeProvider;

    public DownloadJobStore(IServiceScopeFactory scopeFactory, TimeSpan ttl, TimeProvider timeProvider)
    {
        _scopeFactory = scopeFactory;
        _ttl = ttl;
        _timeProvider = timeProvider;
    }

    public string CreateJob(long userId, string videoId, string videoTitle, string? videoDescription, string? videoImageUrl, string videoUrl)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YtDbContext>();

        // Idempotency: a double-tap for the same video while the previous request is
        // still active/in-flight returns the same job instead of enqueueing a duplicate.
        var activeId = db.DownloadCommands
            .Where(c => c.UserId == userId && c.VideoId == videoId
                && (c.Status == DownloadCommandStatus.Queued || c.Status == DownloadCommandStatus.Processing))
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
        db.SaveChanges();
        return command.Id;
    }

    public bool Claim(string tempId, long userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YtDbContext>();

        var affected = db.Database.ExecuteSqlRaw(
            """
            UPDATE "DownloadCommands" SET "Status" = {0}, "NextAttemptAt" = NULL
            WHERE "Id" = {1} AND "UserId" = {2} AND "Status" = {3}
            """,
            DownloadCommandStatus.Processing.ToString(),
            tempId,
            userId,
            DownloadCommandStatus.Queued.ToString());

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
            .Select(c => new { c.Status, c.LastError, c.ErrorCategory })
            .FirstOrDefault();
        if (command is null)
            return null;

        var status = command.Status switch
        {
            DownloadCommandStatus.Queued => DownloadJobStatus.Pending,
            DownloadCommandStatus.Processing => DownloadJobStatus.Running,
            DownloadCommandStatus.Completed => DownloadJobStatus.Ready,
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

    /// <summary>
    /// Crash recovery: any row stuck in Processing was mid-download when the worker
    /// died. Reset it to Queued so a fresh worker picks it up on startup.
    /// </summary>
    internal void ResetOrphanedProcessing()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YtDbContext>();

        db.Database.ExecuteSqlRaw(
            "UPDATE \"DownloadCommands\" SET \"Status\" = {0} WHERE \"Status\" = {1}",
            DownloadCommandStatus.Queued.ToString(),
            DownloadCommandStatus.Processing.ToString());
    }

    /// <summary>
    /// The queue scan. Returns rows that are ready to run: freshly Queued, or Failed
    /// rows whose retry backoff (NextAttemptAt) has elapsed. Ordered oldest-first so a
    /// burst drains FIFO.
    /// </summary>
    internal IReadOnlyList<string> GetEligibleTempIds(int maxCount)
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
    internal bool ClaimEligible(string tempId, long userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YtDbContext>();

        var affected = db.Database.ExecuteSqlRaw(
            """
            UPDATE "DownloadCommands" SET "Status" = {0}, "NextAttemptAt" = NULL
            WHERE "Id" = {1} AND "UserId" = {2}
              AND ("Status" = {3} OR ("Status" = {4} AND "NextAttemptAt" IS NOT NULL AND "NextAttemptAt" <= {5}))
            """,
            DownloadCommandStatus.Processing.ToString(),
            tempId,
            userId,
            DownloadCommandStatus.Queued.ToString(),
            DownloadCommandStatus.Failed.ToString(),
            NowUtc());

        return affected == 1;
    }

    private DateTime NowUtc() => _timeProvider.GetUtcNow().UtcDateTime;

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
