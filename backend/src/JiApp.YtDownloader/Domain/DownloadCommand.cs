using JiApp.Common.Models;

namespace JiApp.YtDownloader.Domain;

public enum DownloadCommandStatus
{
    Queued,
    Processing,
    Completed,
    Failed
}

/// <summary>
/// A durable, retryable download command. The row IS the work queue: the worker
/// claims Queued rows (or Failed rows whose backoff has elapsed), downloads, and
/// marks the row Completed or Failed. An exhausted Failed row is the dead-letter
/// queue — it stays visible through DownloadStatusHandler until the TTL reaps it.
/// </summary>
public sealed class DownloadCommand : BaseEntity<string>
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan[] RetryBackoff = [TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2)];

    public long UserId { get; private set; }
    public string VideoId { get; private set; } = default!;
    public string VideoTitle { get; private set; } = default!;
    public string? VideoDescription { get; private set; }
    public string? VideoImageUrl { get; private set; }
    public string VideoUrl { get; private set; } = default!;
    public DownloadCommandStatus Status { get; private set; }
    public int AttemptsRemaining { get; private set; }
    public string? LastError { get; private set; }
    public string? ErrorCategory { get; private set; }
    public DateTime? NextAttemptAt { get; private set; }
    public DateTime? ProcessingStartedAtUtc { get; private set; }
    public string? FilePath { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private DownloadCommand()
    {
    }

    public static DownloadCommand Create(
        string tempId,
        long userId,
        string videoId,
        string videoTitle,
        string? videoDescription,
        string? videoImageUrl,
        string videoUrl,
        DateTime now,
        TimeSpan ttl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tempId);
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId);
        ArgumentException.ThrowIfNullOrWhiteSpace(videoUrl);

        return new DownloadCommand
        {
            Id = tempId,
            UserId = userId,
            VideoId = videoId,
            VideoTitle = videoTitle,
            VideoDescription = videoDescription,
            VideoImageUrl = videoImageUrl,
            VideoUrl = videoUrl,
            Status = DownloadCommandStatus.Queued,
            AttemptsRemaining = MaxAttempts,
            ExpiresAt = now.Add(ttl),
            CreatedAtUtc = now
        };
    }

    public void MarkReady(string filePath, DateTime now, TimeSpan ttl)
    {
        Status = DownloadCommandStatus.Completed;
        FilePath = filePath;
        ExpiresAt = now.Add(ttl);
        LastError = null;
        ErrorCategory = null;
        NextAttemptAt = null;
        ProcessingStartedAtUtc = null;
    }

    /// <summary>
    /// Records a failed attempt. The first two failures each schedule the next run with
    /// an escalating backoff (30s then 2m); the third failure exhausts the attempts, so
    /// the row becomes the dead-letter record (NextAttemptAt = null) and is never picked
    /// up again.
    /// </summary>
    public void Fail(string error, string? errorCategory, DateTime now)
    {
        Status = DownloadCommandStatus.Failed;
        LastError = error;
        ErrorCategory = errorCategory;
        ProcessingStartedAtUtc = null;

        if (AttemptsRemaining <= 0)
        {
            NextAttemptAt = null;
            return;
        }

        AttemptsRemaining--;

        // Attempts exhausted — this is the dead-letter record; never retry it.
        if (AttemptsRemaining == 0)
        {
            NextAttemptAt = null;
            return;
        }

        var attemptsUsed = MaxAttempts - AttemptsRemaining;
        NextAttemptAt = now.Add(RetryBackoff[Math.Min(attemptsUsed, RetryBackoff.Length) - 1]);
    }
}
