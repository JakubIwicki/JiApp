using System.Collections.Concurrent;

namespace api.JiApp.LovingBoards.Common;

/// <summary>
/// Per-user in-memory write lock to serialise read-modify-write operations on a user's
/// board collection (e.g. the per-owner board cap in CreateBoard). Single-instance —
/// matches the in-memory <see cref="BoardWriteLock"/> assumption; enforced at startup by
/// <see cref="JiApp.Common.Services.SingleInstanceGuard"/>.
/// </summary>
public sealed class UserWriteLock
{
    private readonly ConcurrentDictionary<long, SemaphoreSlim> _locks = new();

    public async Task<IDisposable> AcquireAsync(long userId, CancellationToken ct)
    {
        var sem = _locks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct);
        return new Releaser(sem);
    }

    private sealed class Releaser(SemaphoreSlim sem) : IDisposable
    {
        public void Dispose() => sem.Release();
    }
}
