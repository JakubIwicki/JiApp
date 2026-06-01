using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Scheduler.Features.Boards.GetBoard;

public sealed class GetBoardHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<GetBoardResponse>> HandleAsync(long id, CancellationToken ct)
    {
        var board = await db.Boards
            .Where(b => b.Id == id)
            .Select(b => new GetBoardResponse(b.Id, b.Name, b.MemberUserIds, b.CreatedAt))
            .FirstOrDefaultAsync(ct);

        if (board is null)
            return Result<GetBoardResponse>.Failure("Board not found", ResultCategories.NotFound);

        if (!board.MemberUserIds.Contains(currentUser
                .UserId)) // Inline access check on projected DTO; BoardAccessGuard not used to avoid extra DB query
            return Result<GetBoardResponse>.Failure("Access denied", ResultCategories.AccessDenied);

        return Result<GetBoardResponse>.Success(board);
    }
}