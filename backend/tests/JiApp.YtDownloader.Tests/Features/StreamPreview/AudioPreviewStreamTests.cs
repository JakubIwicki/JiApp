using System.Diagnostics;
using JiApp.YtDownloader.Features.StreamPreview;

namespace JiApp.YtDownloader.Tests.Features.StreamPreview;

public sealed class AudioPreviewStreamTests
{
    [Fact]
    public async Task DisposeAsync_KillsBothProcesses()
    {
        var ytDlp = StartSleepProcess();
        var ffmpeg = StartSleepProcess();
        var ytDlpPid = ytDlp.Id;
        var ffmpegPid = ffmpeg.Id;
        var sut = new AudioPreviewStream(ytDlp, ffmpeg, TimeSpan.FromSeconds(30));

        await sut.DisposeAsync();

        await WaitUntilExitedAsync(ytDlpPid);
        await WaitUntilExitedAsync(ffmpegPid);
    }

    [Fact]
    public async Task DisposeAsync_WhenCalledTwice_IsIdempotent()
    {
        var ytDlp = StartSleepProcess();
        var ffmpeg = StartSleepProcess();
        var ytDlpPid = ytDlp.Id;
        var ffmpegPid = ffmpeg.Id;
        var sut = new AudioPreviewStream(ytDlp, ffmpeg, TimeSpan.FromSeconds(30));

        await sut.DisposeAsync();
        await sut.DisposeAsync();

        await WaitUntilExitedAsync(ytDlpPid);
        await WaitUntilExitedAsync(ffmpegPid);
    }

    [Trait("Category", "Process")]
    [Fact]
    public async Task StartAsync_ThenDisposeAsync_ReturnsPromptlyAndKillsBothProcesses()
    {
        // cat blocks on its redirected stdin, so both processes stay alive with the
        // copy task and stderr drains live — the full hang/leak regression surface.
        var ytDlp = CreateCatProcess();
        var ffmpeg = CreateCatProcess();
        var sut = new AudioPreviewStream(ytDlp, ffmpeg, TimeSpan.FromSeconds(30));

        await sut.StartAsync(CancellationToken.None);

        var ytDlpPid = ytDlp.Id;
        var ffmpegPid = ffmpeg.Id;
        await sut.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));

        await WaitUntilExitedAsync(ytDlpPid);
        await WaitUntilExitedAsync(ffmpegPid);
    }

    [Trait("Category", "Process")]
    [Fact]
    public async Task StartAsync_WhenSecondProcessFailsToStart_KillsStartedProcessAndRethrows()
    {
        var ytDlp = new Process
        {
            StartInfo = new ProcessStartInfo("definitely-not-an-installed-binary")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true,
        };
        var ffmpeg = CreateCatProcess();
        var sut = new AudioPreviewStream(ytDlp, ffmpeg, TimeSpan.FromSeconds(30));

        var act = async () => await sut.StartAsync(CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();

        // ffmpeg started first; its PID is captured at dispose time by the failure path.
        var ffmpegPid = ffmpeg.PidAtDispose
            ?? throw new InvalidOperationException("ffmpeg was not disposed by the failure path.");
        await WaitUntilExitedAsync(ffmpegPid);
    }

    private static Process StartSleepProcess()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo("sleep") { ArgumentList = { "300" } },
            EnableRaisingEvents = true,
        };
        process.Start();
        return process;
    }

    /// <summary>
    /// A not-yet-started <c>cat</c> with the same stdin/stdout/stderr redirection as the
    /// preview pipeline. Once started, cat blocks reading its (never-fed) stdin, keeping it
    /// alive so DisposeAsync exercises the live copy task and stderr drains.
    /// </summary>
    private static PidCapturingProcess CreateCatProcess()
    {
        var process = new PidCapturingProcess
        {
            StartInfo = new ProcessStartInfo("cat")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true,
        };
        return process;
    }

    /// <summary>
    /// Captures the process id the moment the SUT disposes it. After <see cref="Process.Dispose"/>
    /// the id is no longer readable on the object, so this is the only way to observe which
    /// process the start-failure path killed.
    /// </summary>
    private sealed class PidCapturingProcess : Process
    {
        public int? PidAtDispose { get; private set; }

        protected override void Dispose(bool disposing)
        {
            try { PidAtDispose = Id; }
            catch { }
            base.Dispose(disposing);
        }
    }

    private static bool IsRunning(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static async Task WaitUntilExitedAsync(int pid, int attempts = 200)
    {
        for (var i = 0; i < attempts; i++)
        {
            if (!IsRunning(pid))
                return;
            await Task.Delay(25);
        }

        throw new TimeoutException($"Process {pid} did not exit within the timeout.");
    }
}
