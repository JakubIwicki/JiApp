namespace JiApp.YtDownloader.Services;

public sealed class TempFileCleanupService(IDownloadJobStore jobStore, ILogger<TempFileCleanupService> logger)
    : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                jobStore.CleanupExpired();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cleanup expired temp files failed");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
