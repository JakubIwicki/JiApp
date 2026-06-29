using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using api.JiApp.LovingBoards.Configuration;
using api.JiApp.LovingBoards.Domain;
using api.JiApp.LovingBoards.Features.Common;
using api.JiApp.LovingBoards.Persistence;
using api.JiApp.LovingBoards.Realtime;
using Microsoft.EntityFrameworkCore;

namespace api.JiApp.LovingBoards.Features.Items.CreateItem;

public sealed class CreateItemHandler(
    ILovingBoardsDbContext db,
    LovingBoardsSettings settings,
    ICurrentUserService currentUser,
    IBoardBroadcaster broadcaster)
{
    public async Task<Result<long>> HandleAsync(long boardId, CreateItemRequest request, CancellationToken ct)
    {
        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, boardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<long>.Failure(boardResult.Error!, boardResult.ErrorCategory);

        var nonRemovedCount = await db.BoardItems
            .CountAsync(i => i.BoardId == boardId && i.Status != BoardItemStatus.Removed, ct);

        if (nonRemovedCount >= settings.MaxItemsPerBoard)
            return Result<long>.Failure(
                $"Maximum number of items ({settings.MaxItemsPerBoard}) reached for this board",
                ResultCategories.Validation);

        var item = new BoardItem
        {
            BoardId = boardId,
            Title = request.Title,
            Quantity = request.Quantity,
            Category = request.Category,
            Note = request.Note,
            AssigneeUserId = request.AssigneeUserId,
            ExpiryDate = request.ExpiryDate,
            IsRecurring = request.IsRecurring,
            Status = BoardItemStatus.Needed,
            AddedByUserId = currentUser.UserId
        };
        db.BoardItems.Add(item);
        await db.SaveChangesAsync(ct);

        broadcaster.Publish(boardId, new BoardEvent(BoardEventNames.ItemAdded, new { itemId = item.Id }));

        return Result<long>.Success(item.Id);
    }
}
