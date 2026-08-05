using System.Collections.Concurrent;

namespace JiApp.Common.Services;

/// <summary>
/// Fail-fast guard pinning each JiApp deployable to a single instance. Acquires an exclusive
/// file lease inside the shared <c>jiapp_data</c> volume. A second PROCESS (replica) attempting
/// to acquire the same lease throws <see cref="IOException"/>, which the caller treats as fatal
/// (log + exit), so a duplicate replica crash-loops instead of silently sharing state. Within one
/// process the lease is idempotent: re-acquiring the same service name returns the already-held
/// stream (the ASP.NET test host re-runs <c>Program.Main</c> once per WebApplicationFactory).
/// <para>
/// This detects a second instance ONLY on the same host / shared volume; a second EC2 mounting
/// no shared volume is NOT detected. It is a fail-fast misconfiguration guard, not a
/// multi-instance solution.
/// </para>
/// </summary>
public static class SingleInstanceGuard
{
    // Process-lifetime registry: the OS releases the file lease only when the process dies, so the
    // held stream must survive for the whole process. Keeping it here (rather than in the caller's
    // local) is what pins the lease for process lifetime and makes re-acquisition idempotent.
    private static readonly ConcurrentDictionary<string, FileStream> Leases = new();

    /// <summary>
    /// Acquires the process-lifetime exclusive lease for <paramref name="serviceName"/>. Returns the
    /// same stream on subsequent calls within this process; a second PROCESS on the same host/shared
    /// volume fails fast with <see cref="IOException"/>.
    /// </summary>
    /// <exception cref="IOException">Another process already holds the lease.</exception>
    public static FileStream Acquire(string serviceName) => AcquireAt(GetLockPath(serviceName));

    internal static FileStream AcquireAt(string lockPath) =>
        Leases.GetOrAdd(lockPath, static path => OpenExclusive(path));

    /// <summary>
    /// Raw exclusive open of <paramref name="lockPath"/> — the second open of the same path (same or
    /// another process) throws <see cref="IOException"/>. Exposed for tests that probe the lock itself.
    /// </summary>
    internal static FileStream OpenExclusive(string lockPath)
    {
        var directory = Path.GetDirectoryName(lockPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }

    internal static string GetLockPath(string serviceName)
    {
        // Containers run with WORKDIR /app and mount the shared jiapp_data volume at /app/data
        // (docker-compose.prod.yml). AppContext.BaseDirectory is /app/, so BaseDirectory/data
        // lands inside the shared volume — every replica of the same service contends on ONE lock
        // file. In dev/test the path falls under the app's bin directory and is harmless.
        return Path.Combine(AppContext.BaseDirectory, "data", $"{serviceName}.instance.lock");
    }
}
