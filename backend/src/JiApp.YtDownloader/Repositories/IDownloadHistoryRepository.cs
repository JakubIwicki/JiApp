using JiApp.Common.Models;

namespace JiApp.YtDownloader.Repositories;

public interface IDownloadHistoryRepository
{
    Task<IReadOnlyList<YoutubeDownloadHistory>> GetByUserIdAsync(long userId, int limit, int offset = 0);
    Task AddAsync(YoutubeDownloadHistory entry);
    Task<bool> ArchiveAsync(long id, long userId);
    Task SaveChangesAsync();
}