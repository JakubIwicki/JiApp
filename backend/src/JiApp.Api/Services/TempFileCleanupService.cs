using JiApp.Infrastructure.Services;

namespace JiApp.Api.Services;

public sealed class TempFileCleanupService(ITempFileStore tempFileStore, ILogger<TempFileCleanupService> logger)
    : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                tempFileStore.CleanupExpired();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cleanup expired temp files failed");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
