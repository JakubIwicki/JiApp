using JiApp.Common.Models;
using JiApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Infrastructure.Repositories;

public sealed class DownloadHistoryRepository(JiAppDbContext dbContext) : IDownloadHistoryRepository
{
    public async Task<IReadOnlyList<YoutubeDownloadHistory>> GetByUserIdAsync(long userId, int limit, int offset = 0)
    {
        var results = await dbContext.YoutubeDownloadHistory
            .AsNoTracking()
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.DownloadedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return results.AsReadOnly();
    }

    public Task AddAsync(YoutubeDownloadHistory entry)
    {
        dbContext.YoutubeDownloadHistory.Add(entry);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await dbContext.SaveChangesAsync();
    }
}
