namespace JiApp.YtDownloader.Features.StreamPreview;

public interface IAudioPreviewStream : IAsyncDisposable
{
    /// <summary>Returns the mp3 stream produced by ffmpeg's stdout. Call after <see cref="StartAsync"/>.</summary>
    Stream GetAudioStream();

    /// <summary>Starts the yt-dlp and ffmpeg processes and pipes yt-dlp stdout into ffmpeg stdin.</summary>
    Task StartAsync(CancellationToken ct);
}
