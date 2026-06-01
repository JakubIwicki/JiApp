using JiApp.YtApi;
using JiApp.YtDownloader.Configuration;
using JiApp.YtDownloader.Logging;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace JiApp.YtDownloader.Features.StreamPreview;

public sealed class StreamPreviewHandler(
    IYoutubeClient youtubeClient,
    IMemoryCache cache,
    ILogger<StreamPreviewHandler> logger,
    Settings settings)
{
    private const int CacheDurationMinutes = 30;
    private const string CacheKeyPrefix = "youtube:audio-url";

    private static string CacheKey(string videoId) =>
        $"{CacheKeyPrefix}:{videoId}";

    public async Task<StreamPreviewResult> HandleAsync(
        string videoId, CancellationToken cancellationToken = default)
    {
        string audioUrl;

        var key = CacheKey(videoId);
        if (!cache.TryGetValue(key, out string? cachedUrl))
        {
            logger.PreviewCacheMiss(videoId);
            try
            {
                audioUrl = await youtubeClient.ResolveAudioUrlAsync(videoId, cancellationToken);
                cache.Set(key, audioUrl, new MemoryCacheEntryOptions
                {
                    Size = 1,
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheDurationMinutes)
                });
            }
            catch (OperationCanceledException)
            {
                logger.PreviewResolveFailed(new OperationCanceledException("Request was cancelled"), videoId);
                return StreamPreviewResult.ResolveFailed;
            }
            catch (Exception ex)
            {
                logger.PreviewResolveFailed(ex, videoId);
                return StreamPreviewResult.ResolveFailed;
            }
        }
        else
        {
            logger.PreviewCacheHit(videoId);
            audioUrl = cachedUrl!;
        }

        if (!Uri.TryCreate(audioUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            logger.PreviewResolveFailed(new InvalidOperationException($"Invalid audio URL: {audioUrl}"), videoId);
            return StreamPreviewResult.ResolveFailed;
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = BuildFfmpegArguments(audioUrl, settings.App!.PreviewDurationSeconds),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true,
        };

        return new StreamPreviewResult.StreamReady(process);
    }

    internal static string BuildFfmpegArguments(string audioUrl, int previewDurationSeconds) =>
        $"-i \"{audioUrl}\" -t {previewDurationSeconds} -loglevel quiet -f mp3 -";
}

public abstract record StreamPreviewResult
{
    public sealed record StreamReady(Process FfmpegProcess) : StreamPreviewResult;

    public static readonly StreamPreviewResult ResolveFailed = new ResolveFailedRecord();

    private sealed record ResolveFailedRecord : StreamPreviewResult;
}
