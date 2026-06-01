using JiApp.Common.Models;
using JiApp.YtDownloader.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.YtDownloader.Repositories;

public sealed class DownloadHistoryRepository(YtDbContext dbContext) : IDownloadHistoryRepository
{
    public async Task<IReadOnlyList<YoutubeDownloadHistory>> GetByUserIdAsync(long userId, int limit, int offset = 0)
    {
        var results = await dbContext.YoutubeDownloadHistory
            .AsNoTracking()
            .Where(h => h.UserId == userId && !h.IsArchived)
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

    public async Task<bool> ArchiveAsync(long id, long userId)
    {
        var entry = await dbContext.YoutubeDownloadHistory
            .Where(h => h.Id == id && h.UserId == userId)
            .FirstOrDefaultAsync();

        if (entry is null)
            return false;

        entry.IsArchived = true;
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task SaveChangesAsync()
    {
        await dbContext.SaveChangesAsync();
    }
}