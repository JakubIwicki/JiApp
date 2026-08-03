using System.Collections.Concurrent;

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

public sealed class DownloadJobStore : IDownloadJobStore
{
    private sealed record DownloadJobEntry(
        long UserId,
        DownloadJobStatus Status,
        string VideoId,
        string VideoTitle,
        string? VideoDescription,
        string? VideoImageUrl,
        string VideoUrl,
        string? FilePath,
        string? Error,
        string? ErrorCategory,
        DateTime Expiry);

    private readonly TimeSpan _ttl;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, DownloadJobEntry> _store = new();

    public DownloadJobStore(TimeSpan ttl) : this(ttl, TimeProvider.System)
    {
    }

    public DownloadJobStore(TimeSpan ttl, TimeProvider timeProvider)
    {
        _ttl = ttl;
        _timeProvider = timeProvider;
    }

    public string CreateJob(long userId, string videoId, string videoTitle, string? videoDescription, string? videoImageUrl, string videoUrl)
    {
        var tempId = Guid.NewGuid().ToString("N");
        _store[tempId] = new DownloadJobEntry(
            userId,
            DownloadJobStatus.Pending,
            videoId,
            videoTitle,
            videoDescription,
            videoImageUrl,
            videoUrl,
            FilePath: null,
            Error: null,
            ErrorCategory: null,
            Expiry: NowUtc().Add(_ttl));
        return tempId;
    }

    public bool Claim(string tempId, long userId)
    {
        if (!_store.TryGetValue(tempId, out var entry))
            return false;

        if (entry.UserId != userId || entry.Status != DownloadJobStatus.Pending)
            return false;

        return _store.TryUpdate(tempId, entry with { Status = DownloadJobStatus.Running }, entry);
    }

    public void MarkReady(string tempId, long userId, string filePath)
    {
        if (!TryGetOwned(tempId, userId, out var entry))
            return;

        _store.TryUpdate(tempId, entry with
        {
            Status = DownloadJobStatus.Ready,
            FilePath = filePath,
            Expiry = NowUtc().Add(_ttl)
        }, entry);
    }

    public void MarkFailed(string tempId, long userId, string error, string? errorCategory = null)
    {
        if (!TryGetOwned(tempId, userId, out var entry))
            return;

        _store.TryUpdate(tempId, entry with
        {
            Status = DownloadJobStatus.Failed,
            Error = error,
            ErrorCategory = errorCategory
        }, entry);
    }

    public DownloadJobStatusResult? GetStatus(string tempId, long userId)
    {
        if (!TryGetOwned(tempId, userId, out var entry))
            return null;

        return new DownloadJobStatusResult(entry.Status, entry.Error, entry.ErrorCategory);
    }

    // Worker-only lookup keyed on the unguessable temp id; never exposed over HTTP.
    public DownloadJobInfo? GetJobInfo(string tempId)
    {
        if (!_store.TryGetValue(tempId, out var entry))
            return null;

        return new DownloadJobInfo(
            entry.UserId,
            entry.VideoId,
            entry.VideoTitle,
            entry.VideoDescription,
            entry.VideoImageUrl,
            entry.VideoUrl);
    }

    public string? GetFilePath(string tempId, long userId)
    {
        if (!TryGetOwned(tempId, userId, out var entry))
            return null;

        if (entry.Status != DownloadJobStatus.Ready || entry.FilePath is null)
            return null;

        if (NowUtc() > entry.Expiry || !File.Exists(entry.FilePath))
            return null;

        return entry.FilePath;
    }

    public void CleanupExpired()
    {
        var now = NowUtc();

        foreach (var key in _store.Keys.ToList())
        {
            if (!_store.TryGetValue(key, out var entry))
                continue;

            // Never reap a job a worker is actively downloading — MarkReady resets the expiry.
            if (entry.Status == DownloadJobStatus.Running)
                continue;

            if (now <= entry.Expiry)
                continue;

            if (!_store.TryRemove(key, out var removed))
                continue;

            if (removed.Status == DownloadJobStatus.Ready && removed.FilePath is not null)
            {
                TryDeleteFile(removed.FilePath);
            }
        }
    }

    private bool TryGetOwned(string tempId, long userId, out DownloadJobEntry entry)
    {
        if (!_store.TryGetValue(tempId, out entry!))
            return false;

        return entry.UserId == userId;
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
