using JiApp.Common.Abstractions;
using JiApp.YtApi.Clients;
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
    public Result<IAudioPreviewStream> Handle(string videoId)
    {
        Process ytDlp;
        try
        {
            ytDlp = youtubeClient.BuildPreviewAudioProcess(videoId);
        }
        catch (ArgumentException ex)
        {
            logger.PreviewResolveFailed(ex, videoId);
            return Result<IAudioPreviewStream>.Failure(
                "Could not resolve audio for this video. It may be unavailable or age-restricted.",
                ResultCategories.NotFound);
        }

        var previewDurationSeconds = settings.App!.PreviewDurationSeconds;

        var ffmpeg = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = BuildFfmpegArguments(previewDurationSeconds),
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true,
        };

        // The stream's hang timeout mirrors the preview length: give the pipeline a small
        // grace period beyond the trimmed audio before killing the processes.
        return Result<IAudioPreviewStream>.Success(new AudioPreviewStream(
            ytDlp, ffmpeg, TimeSpan.FromSeconds(previewDurationSeconds + 2)));
    }

    internal static string BuildFfmpegArguments(int previewDurationSeconds) =>
        $"-i pipe:0 -t {previewDurationSeconds} -loglevel quiet -f mp3 -";
}
