using JiApp.Common.Models;
using JiApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Infrastructure.Repositories;

public sealed class SearchHistoryRepository(JiAppDbContext dbContext) : ISearchHistoryRepository
{
    public async Task<IReadOnlyList<YoutubeSearchHistory>> GetByUserIdAsync(long userId, int limit, int offset = 0)
    {
        var results = await dbContext.YoutubeSearchHistory
            .AsNoTracking()
            .Where(h => h.UserId == userId)
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

    public async Task SaveChangesAsync()
    {
        await dbContext.SaveChangesAsync();
    }
}
