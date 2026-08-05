using JiApp.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JiApp.Identity.Services;

public sealed class RefreshTokenCleanupService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<RefreshTokenCleanupService> logger)
    : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(CleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                await SweepExpiredAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A failed sweep (e.g. SQLITE_BUSY under concurrent writes) must not take down the
                // host — the default BackgroundServiceExceptionBehavior is StopHost. Log and keep
                // the hourly loop alive.
                logger.LogError(ex, "Refresh token cleanup sweep failed");
            }
        }
    }

    internal async Task SweepExpiredAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        await dbContext.RefreshTokens
            .Where(rt => rt.ExpiresAt < timeProvider.GetUtcNow().UtcDateTime || rt.IsRevoked)
            .ExecuteDeleteAsync(ct);
    }
}
