using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using api.JiApp.LovingBoards.Persistence;
using Microsoft.EntityFrameworkCore;

namespace api.JiApp.LovingBoards.Features.Boards.GetBoard;

public sealed class GetBoardHandler(ILovingBoardsDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<GetBoardResponse>> HandleAsync(long id, CancellationToken ct)
    {
        var board = await db.Boards
            .Where(b => b.Id == id)
            .Select(b => new GetBoardResponse(b.Id, b.Name, b.OwnerUserId, b.MemberUserIds, b.CreatedAt))
            .FirstOrDefaultAsync(ct);

        if (board is null)
            return Result<GetBoardResponse>.Failure("Board not found", ResultCategories.NotFound);

        if (!board.MemberUserIds.Contains(currentUser.UserId))
            return Result<GetBoardResponse>.Failure("Access denied", ResultCategories.AccessDenied);

        return Result<GetBoardResponse>.Success(board);
    }
}
