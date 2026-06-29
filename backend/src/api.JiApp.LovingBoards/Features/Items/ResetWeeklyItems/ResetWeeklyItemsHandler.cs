using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using api.JiApp.LovingBoards.Features.Common;
using api.JiApp.LovingBoards.Persistence;
using api.JiApp.LovingBoards.Realtime;
using Microsoft.EntityFrameworkCore;

namespace api.JiApp.LovingBoards.Features.Items.ResetWeeklyItems;

public sealed class ResetWeeklyItemsHandler(ILovingBoardsDbContext db, ICurrentUserService currentUser, IBoardBroadcaster broadcaster)
{
    public async Task<Result<int>> HandleAsync(long boardId, CancellationToken ct)
    {
        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, boardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<int>.Failure(boardResult.Error!, boardResult.ErrorCategory);

        var board = boardResult.Value!;
        var items = await db.BoardItems
            .Where(i => i.BoardId == boardId)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var count = WeeklyReset.ResetRecurring(items, now);
        board.LastWeeklyResetAt = now;
        await db.SaveChangesAsync(ct);

        broadcaster.Publish(boardId, new BoardEvent(BoardEventNames.RecurringReset, new { reset = count }));

        return Result<int>.Success(count);
    }
}
