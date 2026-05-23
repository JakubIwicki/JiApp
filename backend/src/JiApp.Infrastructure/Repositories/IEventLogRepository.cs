using JiApp.Common.Models;

namespace JiApp.Infrastructure.Repositories;

public interface IEventLogRepository
{
    Task AddAsync(EventLog entry);
    Task SaveChangesAsync();
}