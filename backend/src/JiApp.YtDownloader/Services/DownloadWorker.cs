using System.Threading.Channels;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.YtApi.Clients;
using JiApp.YtApi.Contracts;
using JiApp.YtDownloader.Configuration;
using JiApp.YtDownloader.Logging;
using JiApp.YtDownloader.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JiApp.YtDownloader.Services;

internal sealed class DownloadWorker(
    IDownloadJobStore jobStore,
    IDownloadQueue downloadQueue,
    Channel<string> wakeSignal,
    IYoutubeClient youtubeClient,
    IServiceScopeFactory scopeFactory,
    Settings settings,
    ILogger<DownloadWorker> logger,
    TimeProvider? timeProvider = null,
    TimeSpan? downloadTimeout = null,
    TimeSpan? pollInterval = null) : BackgroundService
{
    private const int MaxConcurrentDownloads = 3;
    private const int HistoryWriteMaxAttempts = 3;
    private static readonly TimeSpan HistoryWriteRetryDelay = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(2);

    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly TimeSpan _downloadTimeout = downloadTimeout
        ?? TimeSpan.FromMinutes(settings.App?.DownloadJobTimeoutMinutes ?? 30);
    private readonly TimeSpan _pollInterval = pollInterval ?? DefaultPollInterval;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Any row still Processing at startup is an orphan from a crashed/killed worker
        // — put it back on the queue so this restart retries it.
        downloadQueue.ResetOrphanedProcessing();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var tempIds = downloadQueue.GetEligibleTempIds(MaxConcurrentDownloads);

                if (tempIds.Count == 0)
                {
                    await WaitForSignalOrPollAsync(stoppingToken);
                    continue;
                }

                await Parallel.ForEachAsync(
                    tempIds,
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
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Download worker iteration failed");
                await Task.Delay(_pollInterval, stoppingToken);
            }
        }
    }

    private async Task ProcessJobAsync(string tempId, CancellationToken stoppingToken)
    {
        var job = jobStore.GetJobInfo(tempId);
        if (job is null)
            return;

        if (!downloadQueue.ClaimEligible(tempId, job.UserId))
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
            downloadResult = await youtubeClient.DownloadVideoAsync(job.VideoId, outputFolder, tempId, timeoutCts.Token);
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
            jobStore.MarkFailed(tempId, job.UserId, "Failed to download video. Please try again later.", ResultCategories.YoutubeDl);
            return;
        }

        var filePath = downloadResult.FilePath;
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            jobStore.MarkFailed(tempId, job.UserId, "Download completed but file is missing.", ResultCategories.YoutubeDl);
            return;
        }

        if (new FileInfo(filePath).Length == 0)
        {
            TryDeleteFile(filePath);
            jobStore.MarkFailed(tempId, job.UserId, "Download completed but file is empty.", ResultCategories.YoutubeDl);
            return;
        }

        jobStore.MarkReady(tempId, job.UserId, filePath);

        await RecordHistoryAsync(job, tempId, stoppingToken);
    }

    private async Task RecordHistoryAsync(DownloadJobInfo job, string tempId, CancellationToken ct)
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

                await repository.AddAsync(historyEntry, ct);
                await repository.SaveChangesAsync(ct);
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

    // The channel is a wake-up signal for newly created jobs; the poll interval is what
    // eventually surfaces retry-after-backoff rows. Waiting on either keeps the loop from
    // hammering the database when the queue is idle.
    private async Task WaitForSignalOrPollAsync(CancellationToken stoppingToken)
    {
        using var pollCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        pollCts.CancelAfter(_pollInterval);

        try
        {
            await wakeSignal.Reader.ReadAsync(pollCts.Token);
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            // Poll interval elapsed with no signal — fall through and re-check the queue.
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
