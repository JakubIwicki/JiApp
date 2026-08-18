using System.Diagnostics;
using JiApp.YtApi.Clients;

namespace JiApp.YtDownloader.Tests;

public sealed class YoutubeClientProcessTests
{
    [Fact]
    public async Task DownloadVideoAsync_OnTokenCancel_KillsTheChildProcessTree()
    {
        var tempDir = Directory.CreateTempSubdirectory("ytdl-kill-test-").FullName;
        try
        {
            var pidFile = Path.Combine(tempDir, "ytdl.pid");
            var childPidFile = Path.Combine(tempDir, "child.pid");
            var script = Path.Combine(tempDir, "fake-yt-dlp.sh");
            File.WriteAllText(script, $"""
                #!/bin/sh
                trap '' TERM INT
                echo $$ > "{pidFile}"
                sleep 60 &
                echo $! > "{childPidFile}"
                wait
                """);
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(script,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

            using var sut = new YoutubeClient("fake-key", script, "ffmpeg");
            using var cts = new CancellationTokenSource();

            var downloadTask = sut.DownloadVideoAsync("dQw4w9WgXcQ", tempDir, "job-temp", cts.Token);

            await WaitForFileAsync(pidFile);
            cts.Cancel();

            // The worker depends on cancellation propagating (not being swallowed) so its
            // per-job deadline path marks the job failed. WaitForExitAsync surfaces
            // cancellation as TaskCanceledException, a subclass of OperationCanceledException.
            var exception = await Record.ExceptionAsync(() => downloadTask);

            exception.Should().BeAssignableTo<OperationCanceledException>();
            await AssertProcessExitedAsync(pidFile);
            await AssertProcessExitedAsync(childPidFile);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static async Task WaitForFileAsync(string path)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < TimeSpan.FromSeconds(10))
        {
            if (File.Exists(path))
                return;

            await Task.Delay(20);
        }

        throw new TimeoutException($"Expected the child process to create {path}.");
    }

    // After SIGKILL a process may linger as a zombie until it is reaped; HasExited is true
    // for a zombie, and a fully-reaped pid makes Process.GetProcessById throw ArgumentException.
    // Either state counts as gone, so poll briefly.
    private static async Task AssertProcessExitedAsync(string pidFile)
    {
        var pid = int.Parse(File.ReadAllText(pidFile).Trim());
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            try
            {
                using var process = Process.GetProcessById(pid);
                process.Refresh();
                if (process.HasExited)
                    return;
            }
            catch (ArgumentException)
            {
                return;
            }

            if (stopwatch.Elapsed >= TimeSpan.FromSeconds(5))
                Assert.Fail($"Process {pid} is still alive after the process tree kill.");

            await Task.Delay(50);
        }
    }
}
