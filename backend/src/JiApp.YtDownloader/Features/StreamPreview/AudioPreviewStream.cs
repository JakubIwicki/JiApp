using System.Diagnostics;

namespace JiApp.YtDownloader.Features.StreamPreview;

/// <summary>
/// Owns the yt-dlp and ffmpeg processes that produce the mp3 preview. The caller starts
/// the pipeline via <see cref="StartAsync"/>, reads audio from <see cref="GetAudioStream"/>,
/// and must dispose the instance when the response completes so both processes are killed.
/// </summary>
public sealed class AudioPreviewStream(Process ytDlp, Process ffmpeg, TimeSpan timeout) : IAudioPreviewStream
{
    private CancellationTokenSource? _timeoutCts;
    private CancellationTokenSource? _linkedCts;
    private Task? _copyTask;
    private bool _disposed;

    public Stream GetAudioStream() => ffmpeg.StandardOutput.BaseStream;

    public Task StartAsync(CancellationToken ct)
    {
        bool ffmpegStarted = false, ytDlpStarted = false;

        try
        {
            // ffmpeg first: it blocks reading pipe:0 while yt-dlp spins up, so the pipe
            // buffer never fills before a reader exists.
            ffmpeg.Start();
            ffmpegStarted = true;
            ytDlp.Start();
            ytDlpStarted = true;
        }
        catch (Exception)
        {
            if (ytDlpStarted)
            {
                try { if (!ytDlp.HasExited) ytDlp.Kill(entireProcessTree: true); }
                catch { }
            }
            if (ffmpegStarted)
            {
                try { if (!ffmpeg.HasExited) ffmpeg.Kill(entireProcessTree: true); }
                catch { }
            }
            ytDlp.Dispose();
            ffmpeg.Dispose();
            _disposed = true;
            throw;
        }

        // Drain stderr on background threads to prevent pipe deadlock
        _ = ytDlp.StandardError.ReadToEndAsync();
        _ = ffmpeg.StandardError.ReadToEndAsync();

        _timeoutCts = new CancellationTokenSource(timeout);
        _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _timeoutCts.Token);
        var linkedToken = _linkedCts.Token;

        // Register timeout callback to kill processes if they hang
        linkedToken.Register(KillProcesses);

        // Pipe yt-dlp stdout into ffmpeg stdin on a background task
        _copyTask = Task.Run(async () =>
        {
            try
            {
                await ytDlp.StandardOutput.BaseStream.CopyToAsync(
                    ffmpeg.StandardInput.BaseStream, linkedToken);
            }
            catch (IOException)
            {
                // Broken pipe is expected when ffmpeg stops early after -t N
            }
            catch (OperationCanceledException)
            {
                // Expected on timeout or client disconnect
            }
            finally
            {
                try { ffmpeg.StandardInput.Close(); }
                catch { }
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;

        KillProcesses();

        ytDlp.Dispose();
        ffmpeg.Dispose();

        _timeoutCts?.Cancel();
        _timeoutCts?.Dispose();
        _linkedCts?.Dispose();

        if (_copyTask is not null)
        {
            try { await _copyTask; }
            catch { }
        }
    }

    private void KillProcesses()
    {
        try { if (!ytDlp.HasExited) ytDlp.Kill(entireProcessTree: true); }
        catch { }
        try { if (!ffmpeg.HasExited) ffmpeg.Kill(entireProcessTree: true); }
        catch { }
    }
}
