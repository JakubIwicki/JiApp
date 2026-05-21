using System.Collections.Concurrent;

namespace JiApp.Infrastructure.Services;

public sealed class TempFileStore(TimeSpan lifetime) : ITempFileStore
{
    private readonly ConcurrentDictionary<string, (string Path, DateTime Expiry)> _store = new();

    public TempFileStore() : this(TimeSpan.FromMinutes(10))
    {
    }

    public string Add(string filePath)
    {
        var id = Guid.NewGuid().ToString("N");
        var expiry = DateTime.UtcNow.Add(lifetime);
        _store[id] = (filePath, expiry);
        return id;
    }

    public string? Get(string id)
    {
        if (!_store.TryGetValue(id, out var entry))
            return null;
        
        if (DateTime.UtcNow <= entry.Expiry && File.Exists(entry.Path))
        {
            return entry.Path;
        }

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
            
            if (File.Exists(entry.Path))
            {
                File.Delete(entry.Path);
            }
        }
    }
}
