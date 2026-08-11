using System.Runtime.CompilerServices;
using JiApp.Common.Services;

namespace JiApp.Gateway.Tests.Integration.CrossModule;

/// <summary>
/// Pre-warms the single-instance leases for every host this assembly can boot at
/// assembly load, before any test constructs a host.
///
/// Each host's Program.Main calls <see cref="SingleInstanceGuard.Acquire"/>. Under
/// parallel xUnit classes (the CrossModule fixture and the existing Gateway
/// integration tests boot hosts simultaneously), SingleInstanceGuard's
/// ConcurrentDictionary.GetOrAdd runs its OpenExclusive factory concurrently under
/// contention — the loser throws IOException, Program.Main calls Environment.Exit(1),
/// and the whole test process dies ("host process crashed"). A module initializer runs
/// before any test constructs a host, so every later Acquire hits the already-cached
/// lease and no factory ever races.
/// </summary>
internal static class SingleInstanceGuardPreWarm
{
    [ModuleInitializer]
    internal static void PreWarm()
    {
        SingleInstanceGuard.Acquire("gateway");
        SingleInstanceGuard.Acquire("identity");
        SingleInstanceGuard.Acquire("scheduler");
        SingleInstanceGuard.Acquire("lovingboards");
        SingleInstanceGuard.Acquire("ytdownloader");
    }
}
