using JiApp.YtApi;
using JiApp.YtDownloader.Configuration;
using JiApp.YtDownloader.Logging;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace JiApp.YtDownloader.Features.StreamPreview;

public sealed class StreamPreviewHandler(
    IYoutubeClient youtubeClient,
    ILogger<StreamPreviewHandler> logger,
    Settings settings)
{
    public StreamPreviewResult Handle(string videoId)
    {
        Process ytDlp;
        try
        {
            ytDlp = youtubeClient.BuildPreviewAudioProcess(videoId);
        }
        catch (ArgumentException ex)
        {
            logger.PreviewResolveFailed(ex, videoId);
            return StreamPreviewResult.ResolveFailed;
        }

        var ffmpeg = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = BuildFfmpegArguments(settings.App!.PreviewDurationSeconds),
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true,
        };

        return new StreamPreviewResult.StreamReady(ytDlp, ffmpeg);
    }

    internal static string BuildFfmpegArguments(int previewDurationSeconds) =>
        $"-i pipe:0 -t {previewDurationSeconds} -loglevel quiet -f mp3 -";
}

public abstract record StreamPreviewResult
{
    public sealed record StreamReady(Process YtDlpProcess, Process FfmpegProcess) : StreamPreviewResult;

    public static readonly StreamPreviewResult ResolveFailed = new ResolveFailedRecord();

    private sealed record ResolveFailedRecord : StreamPreviewResult;
}
