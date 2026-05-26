using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using JiApp.Api.Logging;
using JiApp.YtApi;
using Microsoft.Extensions.Logging;

namespace JiApp.Api.Features.Preview.StreamPreview;

public sealed class StreamPreviewHandler(
    IYoutubeClient youtubeClient,
    ILogger<StreamPreviewHandler> logger)
{
    public async Task<StreamPreviewResult> HandleAsync(
        string videoId, CancellationToken cancellationToken = default)
    {
        string audioUrl;
        try
        {
            audioUrl = await youtubeClient.ResolveAudioUrlAsync(videoId);
        }
        catch (Exception ex)
        {
            logger.PreviewResolveFailed(ex, videoId);
            return StreamPreviewResult.ResolveFailed;
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{audioUrl}\" -t 10 -f mp3 -",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true,
        };

        return new StreamPreviewResult.StreamReady(process);
    }
}

public abstract record StreamPreviewResult
{
    public sealed record StreamReady(Process FfmpegProcess) : StreamPreviewResult;

    public static readonly StreamPreviewResult ResolveFailed = new ResolveFailedRecord();

    private sealed record ResolveFailedRecord : StreamPreviewResult;
}