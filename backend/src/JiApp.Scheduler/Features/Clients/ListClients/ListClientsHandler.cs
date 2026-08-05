using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Scheduler.Features.Clients.ListClients;

public sealed class ListClientsHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<List<ClientResponse>>> HandleAsync(string? q, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        var uid = currentUser.UserId;
        var userBoardIds = await db.Database
            .SqlQuery<long>(MemberBoardIdsSql(uid))
            .ToListAsync(ct);

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

    private static FormattableString MemberBoardIdsSql(long userId)
    {
        var single = $"%[{userId}]%";
        var leading = $"%[{userId},%";
        var middle = $"%,{userId},%";
        var trailing = $"%,{userId}]%";
        return $"""
            SELECT "Id" FROM "Boards"
            WHERE "OwnerUserId" = {userId}
               OR "MemberUserIds" LIKE {single}
               OR "MemberUserIds" LIKE {leading}
               OR "MemberUserIds" LIKE {middle}
               OR "MemberUserIds" LIKE {trailing}
            """;
    }
}