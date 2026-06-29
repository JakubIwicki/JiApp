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

        if (request.Title.IsSet) item.Title = request.Title.Value!;
        if (request.Quantity.IsSet) item.Quantity = request.Quantity.Value;
        if (request.Category.IsSet) item.Category = request.Category.Value;
        if (request.Note.IsSet) item.Note = request.Note.Value;
        if (request.AssigneeUserId.IsSet) item.AssigneeUserId = request.AssigneeUserId.Value;
        if (request.ExpiryDate.IsSet) item.ExpiryDate = request.ExpiryDate.Value;
        if (request.IsRecurring.IsSet) item.IsRecurring = request.IsRecurring.Value;
        item.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        broadcaster.Publish(boardId, new BoardEvent(BoardEventNames.ItemUpdated, new { itemId = item.Id }));

        return Result<long>.Success(item.Id);
    }
}
