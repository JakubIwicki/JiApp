using System.Threading.Channels;
using JiApp.Common.Models;
using JiApp.YtApi;
using JiApp.YtDownloader.Configuration;
using JiApp.YtDownloader.Logging;
using JiApp.YtDownloader.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JiApp.YtDownloader.Services;

public sealed class DownloadWorker(
    IDownloadJobStore jobStore,
    Channel<string> downloadQueue,
    IYoutubeClient youtubeClient,
    IServiceScopeFactory scopeFactory,
    Settings settings,
    ILogger<DownloadWorker> logger,
    TimeProvider? timeProvider = null,
    TimeSpan? downloadTimeout = null) : BackgroundService
{
    public const string YoutubeDlErrorCategory = "YoutubeDl";

    private const int MaxConcurrentDownloads = 3;
    private const int HistoryWriteMaxAttempts = 3;
    private static readonly TimeSpan HistoryWriteRetryDelay = TimeSpan.FromMilliseconds(250);

    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly TimeSpan _downloadTimeout = downloadTimeout
        ?? TimeSpan.FromMinutes(settings.App?.DownloadJobTimeoutMinutes ?? 30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Parallel.ForEachAsync(
            downloadQueue.Reader.ReadAllAsync(stoppingToken),
            new ParallelOptions { CancellationToken = stoppingToken, MaxDegreeOfParallelism = MaxConcurrentDownloads },
            async (tempId, ct) =>
            {
                try
                {
                    await ProcessJobAsync(tempId, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Unhandled error processing download job {TempId}", tempId);
                }
            });
    }

    private async Task ProcessJobAsync(string tempId, CancellationToken stoppingToken)
    {
        var job = jobStore.GetJobInfo(tempId);
        if (job is null)
            return;

        if (!jobStore.Claim(tempId, job.UserId))
            return;

        var outputFolder = Path.Combine(settings.App?.BaseDirectory ?? "/tmp", $"YtMp3_{job.UserId}");

        // A per-job deadline so a hung yt-dlp/ffmpeg child frees its worker slot
        // instead of pinning it forever. The timeout token is linked to the host
        // lifetime token so shutdown still cancels in-flight downloads.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeoutCts.CancelAfter(_downloadTimeout);

        YoutubeClientResponse downloadResult;
        try
        {
            downloadResult = await youtubeClient.DownloadVideoAsync(job.VideoId, outputFolder, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            jobStore.MarkFailed(tempId, job.UserId, "Download timed out.");
            return;
        }
        catch (Exception ex)
        {
            logger.DownloadFailedForVideo(ex, job.VideoId);
            jobStore.MarkFailed(tempId, job.UserId, "Failed to process download. Please try again later.");
            return;
        }

        if (!downloadResult.Success)
        {
            var errors = string.Join(", ", downloadResult.Errors);
            logger.YoutubeDlDownloadFailed(job.VideoId, errors);
            jobStore.MarkFailed(tempId, job.UserId, "Failed to download video. Please try again later.", YoutubeDlErrorCategory);
            return;
        }

        var filePath = downloadResult.FilePath;
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            jobStore.MarkFailed(tempId, job.UserId, "Download completed but file is missing.", YoutubeDlErrorCategory);
            return;
        }

        if (new FileInfo(filePath).Length == 0)
        {
            TryDeleteFile(filePath);
            jobStore.MarkFailed(tempId, job.UserId, "Download completed but file is empty.", YoutubeDlErrorCategory);
            return;
        }

        jobStore.MarkReady(tempId, job.UserId, filePath);

        await RecordHistoryAsync(job, tempId);
    }

    private async Task RecordHistoryAsync(DownloadJobInfo job, string tempId)
    {
        for (var attempt = 1; attempt <= HistoryWriteMaxAttempts; attempt++)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IDownloadHistoryRepository>();

                var historyEntry = new YoutubeDownloadHistory
                {
                    UserId = job.UserId,
                    DownloadedAt = _timeProvider.GetUtcNow().UtcDateTime,
                    VideoTitle = job.VideoTitle,
                    VideoDescription = job.VideoDescription,
                    VideoId = job.VideoId,
                    VideoUrl = job.VideoUrl,
                    ImageUrl = job.VideoImageUrl
                };

                await repository.AddAsync(historyEntry);
                await repository.SaveChangesAsync();
                return;
            }
            catch (Exception ex) when (attempt < HistoryWriteMaxAttempts)
            {
                await Task.Delay(HistoryWriteRetryDelay);
                logger.LogWarning(ex, "Failed to record download history for job {TempId} (attempt {Attempt}/{MaxAttempts})",
                    tempId, attempt, HistoryWriteMaxAttempts);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to record download history for job {TempId} after {MaxAttempts} attempts",
                    tempId, HistoryWriteMaxAttempts);
            }
        }
    }

    private static void TryDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            // File may be locked or protected — skip and continue cleanup
        }
    }
}
