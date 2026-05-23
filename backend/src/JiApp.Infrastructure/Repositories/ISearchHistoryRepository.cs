using JiApp.Common.Models;

namespace JiApp.Infrastructure.Repositories;

public interface ISearchHistoryRepository
{
    Task<IReadOnlyList<YoutubeSearchHistory>> GetByUserIdAsync(long userId, int limit, int offset = 0);
    Task AddAsync(YoutubeSearchHistory entry);
    Task SaveChangesAsync();
}