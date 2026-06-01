using JiApp.Common.Models;
using JiApp.YtDownloader.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.YtDownloader.Repositories;

public sealed class SearchHistoryRepository(YtDbContext dbContext) : ISearchHistoryRepository
{
    public async Task<IReadOnlyList<YoutubeSearchHistory>> GetByUserIdAsync(long userId, int limit, int offset = 0)
    {
        var results = await dbContext.YoutubeSearchHistory
            .AsNoTracking()
            .Where(h => h.UserId == userId && !h.IsArchived)
            .OrderByDescending(h => h.SearchedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return results.AsReadOnly();
    }

    public Task AddAsync(YoutubeSearchHistory entry)
    {
        dbContext.YoutubeSearchHistory.Add(entry);
        return Task.CompletedTask;
    }

    public async Task<bool> ArchiveAsync(long id, long userId)
    {
        var entry = await dbContext.YoutubeSearchHistory
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