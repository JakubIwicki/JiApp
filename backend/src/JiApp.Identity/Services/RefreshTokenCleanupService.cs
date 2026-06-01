using JiApp.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Identity.Services;

public sealed class RefreshTokenCleanupService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CleanupInterval, stoppingToken);

            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

            await dbContext.RefreshTokens
                .Where(rt => rt.ExpiresAt < DateTime.UtcNow || rt.IsRevoked)
                .ExecuteDeleteAsync(stoppingToken);
        }
    }
}