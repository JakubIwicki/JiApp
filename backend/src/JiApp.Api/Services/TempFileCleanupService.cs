using System;
using System.Threading;
using System.Threading.Tasks;
using JiApp.Api.Logging;
using JiApp.Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
                logger.CleanupExpiredTempFilesFailed(ex);
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}