using System.Collections.Concurrent;

namespace api.JiApp.LovingBoards.Common;

/// <summary>
/// Per-board in-memory write lock to serialise read-modify-write operations on a board's
/// <c>MemberUserIds</c> JSON list. Single-instance — matches the in-memory
/// <see cref="Realtime.IBoardBroadcaster"/> assumption.
/// </summary>
public sealed class BoardWriteLock
{
    private readonly ConcurrentDictionary<long, SemaphoreSlim> _locks = new();

    public async Task<IDisposable> AcquireAsync(long boardId, CancellationToken ct)
    {
        var sem = _locks.GetOrAdd(boardId, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct);
        return new Releaser(sem);
    }

    private sealed class Releaser(SemaphoreSlim sem) : IDisposable
    {
        public void Dispose() => sem.Release();
    }
}
