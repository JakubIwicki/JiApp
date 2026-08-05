using System.IO;

namespace JiApp.Common.Tests.Conventions;

public sealed class SingleInstanceGuardConventionTests
{
    // Every service deployable (NOT JiApp.ImageTools — it is not a service deployable)
    // must acquire the single-instance lease in its Program.cs before serving traffic.
    private static readonly string[] DeployableProgramFiles =
    [
        "src/JiApp.Gateway/Program.cs",
        "src/JiApp.Identity/Program.cs",
        "src/JiApp.YtDownloader/Program.cs",
        "src/JiApp.Scheduler/Program.cs",
        "src/api.JiApp.LovingBoards/Program.cs"
    ];

    [Fact]
    public void AllDeployablePrograms_AcquireSingleInstanceGuard()
    {
        var backendDir = FindBackendDir();
        var violations = new List<string>();

        foreach (var relativePath in DeployableProgramFiles)
        {
            var fullPath = Path.Combine(backendDir, relativePath);
            if (!File.Exists(fullPath))
            {
                violations.Add($"{relativePath} — file not found");
                continue;
            }

            var source = File.ReadAllText(fullPath);
            if (!source.Contains("SingleInstanceGuard.Acquire", StringComparison.Ordinal))
                violations.Add($"{relativePath} — no SingleInstanceGuard.Acquire call");
        }

        Assert.True(DeployableProgramFiles.Length > 0,
            "0 deployable Program.cs files scanned — the fitness test ran vacuously");
        Assert.True(violations.Count == 0,
            $"The following {violations.Count} deployable(s) lack the single-instance guard:\n" +
            string.Join("\n", violations));
    }

    private static string FindBackendDir()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "JiApp.sln")))
                return current;

            current = Path.GetDirectoryName(current);
        }

        throw new InvalidOperationException(
            $"Could not locate JiApp.sln walking up from {AppContext.BaseDirectory}.");
    }
}
