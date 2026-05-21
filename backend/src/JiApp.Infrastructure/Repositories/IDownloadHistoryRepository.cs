using JiApp.Common.Models;

namespace JiApp.Infrastructure.Repositories;

public interface IDownloadHistoryRepository
{
    Task<IReadOnlyList<YoutubeDownloadHistory>> GetByUserIdAsync(long userId, int limit, int offset = 0);
    Task AddAsync(YoutubeDownloadHistory entry);
    Task SaveChangesAsync();
}
