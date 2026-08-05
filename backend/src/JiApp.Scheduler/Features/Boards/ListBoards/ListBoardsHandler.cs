using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Boards.GetBoard;
using JiApp.Scheduler.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Scheduler.Features.Boards.ListBoards;

public sealed class ListBoardsHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<ListBoardsResponse>> HandleAsync(CancellationToken ct)
    {
        var boards = await db.Boards
            .FromSqlInterpolated(MemberBoardsSql(currentUser.UserId))
            .AsNoTracking()
            .Select(b => new GetBoardResponse(b.Id, b.Name, b.OwnerUserId, b.MemberUserIds, b.CreatedAt))
            .ToListAsync(ct);

        return Result<ListBoardsResponse>.Success(new ListBoardsResponse(boards));
    }

    private static FormattableString MemberBoardsSql(long userId)
    {
        var single = $"%[{userId}]%";
        var leading = $"%[{userId},%";
        var middle = $"%,{userId},%";
        var trailing = $"%,{userId}]%";
        return $"""
            SELECT * FROM "Boards"
            WHERE "OwnerUserId" = {userId}
               OR "MemberUserIds" LIKE {single}
               OR "MemberUserIds" LIKE {leading}
               OR "MemberUserIds" LIKE {middle}
               OR "MemberUserIds" LIKE {trailing}
            """;
    }
}