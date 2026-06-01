using System.Collections.Concurrent;

namespace JiApp.YtDownloader.Services;

public interface ITempFileStore
{
    string Add(string filePath, long userId);
    string? Get(string id, long userId);
    void CleanupExpired();
}

public sealed class TempFileStore(TimeSpan lifetime) : ITempFileStore
{
    private sealed record FileEntry(string Path, DateTime Expiry, long UserId);

    private readonly ConcurrentDictionary<string, FileEntry> _store = new();

    public TempFileStore() : this(TimeSpan.FromMinutes(10))
    {
    }

    public string Add(string filePath, long userId)
    {
        var id = Guid.NewGuid().ToString("N");
        var expiry = DateTime.UtcNow.Add(lifetime);
        _store[id] = new FileEntry(filePath, expiry, userId);
        return id;
    }

    public string? Get(string id, long userId)
    {
        if (!_store.TryGetValue(id, out var entry))
            return null;

        if (entry.UserId != userId)
            return null;

        if (DateTime.UtcNow <= entry.Expiry && File.Exists(entry.Path))
        {
            return entry.Path;
        }

        // Remove expired entries to prevent memory leak
        _store.TryRemove(id, out _);
        return null;
    }

    public void CleanupExpired()
    {
        var now = DateTime.UtcNow;

        var expired = _store
            .Where(kvp => now > kvp.Value.Expiry)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expired)
        {
            if (!_store.TryRemove(key, out var entry))
                continue;

            try
            {
                if (File.Exists(entry.Path))
                {
                    File.Delete(entry.Path);
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                // File may be locked or protected — skip and continue cleanup
            }
        }
    }
}