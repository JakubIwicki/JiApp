using JiApp.Common.Abstractions;
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
    public Result<StreamReady> Handle(string videoId)
    {
        Process ytDlp;
        try
        {
            ytDlp = youtubeClient.BuildPreviewAudioProcess(videoId);
        }
        catch (ArgumentException ex)
        {
            logger.PreviewResolveFailed(ex, videoId);
            return Result<StreamReady>.Failure(
                "Could not resolve audio for this video. It may be unavailable or age-restricted.",
                ResultCategories.NotFound);
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

        return Result<StreamReady>.Success(new StreamReady(ytDlp, ffmpeg));
    }

    internal static string BuildFfmpegArguments(int previewDurationSeconds) =>
        $"-i pipe:0 -t {previewDurationSeconds} -loglevel quiet -f mp3 -";
}

public sealed record StreamReady(Process YtDlpProcess, Process FfmpegProcess);
