using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Scheduler.Features.Clients.ListClients;

public sealed class ListClientsHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<List<ClientResponse>>> HandleAsync(string? q, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        var userBoardIds = (await db.Boards
                .AsNoTracking()
                .ToListAsync(ct))
            .Where(b => b.MemberUserIds.Contains(currentUser.UserId))
            .Select(b => b.Id)
            .ToList();

        var query = db.Clients
            .Where(c => userBoardIds.Contains(c.BoardId));

        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(search));
        }

        var clients = await query
            .OrderBy(c => c.Name)
            .Skip(skip)
            .Take(take)
            .Select(c => new ClientResponse(c.Id, c.BoardId, c.Name, c.Phone, c.Notes))
            .ToListAsync(ct);
        return Result<List<ClientResponse>>.Success(clients);
    }
}