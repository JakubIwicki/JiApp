using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using api.JiApp.LovingBoards.Features.Common;
using api.JiApp.LovingBoards.Persistence;
using api.JiApp.LovingBoards.Realtime;
using Microsoft.EntityFrameworkCore;

namespace api.JiApp.LovingBoards.Features.Items.DeleteItem;

public sealed class DeleteItemHandler(ILovingBoardsDbContext db, ICurrentUserService currentUser, IBoardBroadcaster broadcaster)
{
    public async Task<Result<long>> HandleAsync(long boardId, long itemId, CancellationToken ct)
    {
        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, boardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<long>.Failure(boardResult.Error!, boardResult.ErrorCategory);

        var item = await db.BoardItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.BoardId == boardId, ct);

        if (item is null)
            return Result<long>.Failure("Item not found", ResultCategories.NotFound);

        db.BoardItems.Remove(item);
        await db.SaveChangesAsync(ct);

        broadcaster.Publish(boardId, new BoardEvent(BoardEventNames.ItemRemoved, new { itemId }));

        return Result<long>.Success(itemId);
    }
}
