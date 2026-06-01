using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Domain;
using JiApp.Scheduler.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Scheduler.Features.Common;

internal static class BoardAccessGuard
{
    internal static async Task<Result<Board>> VerifyBoardAccessAsync(
        ISchedulerDbContext db, long boardId, ICurrentUserService currentUser, CancellationToken ct)
    {
        var board = await db.Boards.FindAsync([boardId], ct);
        if (board is null)
            return Result<Board>.Failure("Board not found", ResultCategories.NotFound);

        if (!board.MemberUserIds.Contains(currentUser.UserId))
            return Result<Board>.Failure("Access denied", ResultCategories.AccessDenied);

        return Result<Board>.Success(board);
    }
}