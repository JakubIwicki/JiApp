using JiApp.Common.Models;
using JiApp.Infrastructure.Persistence;

namespace JiApp.Infrastructure.Repositories;

public sealed class EventLogRepository(JiAppDbContext dbContext) : IEventLogRepository
{
    public Task AddAsync(EventLog entry)
    {
        dbContext.EventLogs.Add(entry);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await dbContext.SaveChangesAsync();
    }
}