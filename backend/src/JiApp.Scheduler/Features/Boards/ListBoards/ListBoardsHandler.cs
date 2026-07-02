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
            .AsNoTracking()
            .ToListAsync(ct);

        var userBoards = boards
            .Where(b => b.MemberUserIds.Contains(currentUser.UserId))
            .Select(b => new GetBoardResponse(b.Id, b.Name, b.OwnerUserId, b.MemberUserIds, b.CreatedAt))
            .ToList();

        return Result<ListBoardsResponse>.Success(new ListBoardsResponse(userBoards));
    }
}