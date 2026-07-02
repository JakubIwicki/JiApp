using api.JiApp.LovingBoards.Domain;
using api.JiApp.LovingBoards.Persistence;
using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using Microsoft.EntityFrameworkCore;

namespace api.JiApp.LovingBoards.Features.Common;

internal static class BoardAccessGuard
{
    internal static async Task<Result<Board>> VerifyBoardAccessAsync(
        ILovingBoardsDbContext db, long boardId, ICurrentUserService currentUser, CancellationToken ct)
    {
        var board = await db.Boards.FindAsync([boardId], ct);
        if (board is null)
            return Result<Board>.Failure("Board not found", ResultCategories.NotFound);

        if (!board.MemberUserIds.Contains(currentUser.UserId))
            return Result<Board>.Failure("Access denied", ResultCategories.AccessDenied);

        return Result<Board>.Success(board);
    }

    internal static async Task<Result<Board>> VerifyBoardOwnerAsync(
        ILovingBoardsDbContext db, long boardId, ICurrentUserService currentUser, CancellationToken ct)
    {
        var board = await db.Boards.FindAsync([boardId], ct);
        if (board is null)
            return Result<Board>.Failure("Board not found", ResultCategories.NotFound);

        if (board.OwnerUserId != currentUser.UserId)
            return Result<Board>.Failure("Access denied", ResultCategories.AccessDenied);

        return Result<Board>.Success(board);
    }
}
