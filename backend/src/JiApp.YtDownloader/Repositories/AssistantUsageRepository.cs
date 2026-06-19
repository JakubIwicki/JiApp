using JiApp.Common.Models;
using JiApp.YtDownloader.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace JiApp.YtDownloader.Repositories;

public sealed class AssistantUsageRepository(YtDbContext dbContext) : IAssistantUsageRepository
{
    public async Task<bool> TryConsumeAsync(long userId, int limit, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var existing = await dbContext.AssistantDailyUsage
            .SingleOrDefaultAsync(u => u.UserId == userId && u.UsageDateUtc == today, ct);

        if (existing is not null)
        {
            if (existing.Count >= limit)
                return false;

            existing.Count++;
            await dbContext.SaveChangesAsync(ct);
            return true;
        }

        var inserted = new AssistantDailyUsage
        {
            UserId = userId,
            UsageDateUtc = today,
            Count = 1,
        };
        dbContext.AssistantDailyUsage.Add(inserted);

        try
        {
            await dbContext.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            dbContext.Entry(inserted).State = EntityState.Detached;
            return await IncrementExistingAsync(userId, today, limit, ct);
        }
    }

    private async Task<bool> IncrementExistingAsync(long userId, DateOnly today, int limit, CancellationToken ct)
    {
        var existing = await dbContext.AssistantDailyUsage
            .SingleAsync(u => u.UserId == userId && u.UsageDateUtc == today, ct);

        if (existing.Count >= limit)
            return false;

        existing.Count++;
        await dbContext.SaveChangesAsync(ct);
        return true;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException switch
        {
            SqliteException sqliteEx => sqliteEx.SqliteErrorCode == 19, // SQLITE_CONSTRAINT
            PostgresException postgresEx => postgresEx.SqlState == "23505", // unique_violation
            _ => false,
        };
    }
}
