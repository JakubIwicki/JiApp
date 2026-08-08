using System.Runtime.CompilerServices;
using JiApp.Common.Services;

namespace JiApp.YtDownloader.Tests.Integration;

/// <summary>
/// Pre-warms the YtDownloader single-instance lease at assembly load.
///
/// Every test host constructed in this assembly runs Program.Main, which calls
/// <see cref="SingleInstanceGuard.Acquire"/>. Under parallel xUnit classes, two hosts
/// can start simultaneously, and SingleInstanceGuard's ConcurrentDictionary.GetOrAdd
/// runs its OpenExclusive factory concurrently under contention — the loser throws
/// IOException, Program.Main calls Environment.Exit(1), and the whole test process
/// dies ("host process crashed"). A module initializer runs before any test constructs
/// a host, so every later Acquire hits the already-cached lease and no factory ever
/// races.
/// </summary>
internal static class SingleInstanceGuardPreWarm
{
    [ModuleInitializer]
    internal static void PreWarm()
    {
        SingleInstanceGuard.Acquire("ytdownloader");
    }
}
