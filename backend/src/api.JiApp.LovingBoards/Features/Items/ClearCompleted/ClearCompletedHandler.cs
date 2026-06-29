using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using api.JiApp.LovingBoards.Domain;
using api.JiApp.LovingBoards.Features.Common;
using api.JiApp.LovingBoards.Persistence;
using api.JiApp.LovingBoards.Realtime;
using Microsoft.EntityFrameworkCore;

namespace api.JiApp.LovingBoards.Features.Items.ClearCompleted;

public sealed class ClearCompletedHandler(ILovingBoardsDbContext db, ICurrentUserService currentUser, IBoardBroadcaster broadcaster)
{
    public async Task<Result<int>> HandleAsync(long boardId, CancellationToken ct)
    {
        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, boardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<int>.Failure(boardResult.Error!, boardResult.ErrorCategory);

        var now = DateTime.UtcNow;
        var completedItems = await db.BoardItems
            .Where(i => i.BoardId == boardId && i.Status == BoardItemStatus.Completed)
            .ToListAsync(ct);

        foreach (var item in completedItems)
        {
            item.Status = BoardItemStatus.Removed;
            item.RemovedAt = now;
            item.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);

        if (completedItems.Count > 0)
        {
            var itemIds = completedItems.Select(i => i.Id).ToList();
            broadcaster.Publish(boardId, new BoardEvent(BoardEventNames.ItemsCleared, new { itemIds }));
        }

        return Result<int>.Success(completedItems.Count);
    }
}
