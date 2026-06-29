using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using api.JiApp.LovingBoards.Features.Common;
using api.JiApp.LovingBoards.Persistence;
using api.JiApp.LovingBoards.Realtime;
using Microsoft.EntityFrameworkCore;

namespace api.JiApp.LovingBoards.Features.Items.UpdateItem;

public sealed class UpdateItemHandler(ILovingBoardsDbContext db, ICurrentUserService currentUser, IBoardBroadcaster broadcaster)
{
    public async Task<Result<long>> HandleAsync(long boardId, long itemId, UpdateItemRequest request, CancellationToken ct)
    {
        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, boardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<long>.Failure(boardResult.Error!, boardResult.ErrorCategory);

        var item = await db.BoardItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.BoardId == boardId, ct);

        if (item is null)
            return Result<long>.Failure("Item not found", ResultCategories.NotFound);

        item.Title = request.Title;
        item.Quantity = request.Quantity;
        item.Category = request.Category;
        item.Note = request.Note;
        item.AssigneeUserId = request.AssigneeUserId;
        item.ExpiryDate = request.ExpiryDate;
        item.IsRecurring = request.IsRecurring;
        item.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        broadcaster.Publish(boardId, new BoardEvent(BoardEventNames.ItemUpdated, new { itemId = item.Id }));

        return Result<long>.Success(item.Id);
    }
}
