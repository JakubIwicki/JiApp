using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using api.JiApp.LovingBoards.Domain;
using api.JiApp.LovingBoards.Features.Common;
using api.JiApp.LovingBoards.Persistence;
using api.JiApp.LovingBoards.Realtime;
using Microsoft.EntityFrameworkCore;

namespace api.JiApp.LovingBoards.Features.Items.SetItemStatus;

public sealed class SetItemStatusHandler(ILovingBoardsDbContext db, ICurrentUserService currentUser, IBoardBroadcaster broadcaster)
{
    public async Task<Result<long>> HandleAsync(long boardId, long itemId, SetItemStatusRequest request, CancellationToken ct)
    {
        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, boardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<long>.Failure(boardResult.Error!, boardResult.ErrorCategory);

        var item = await db.BoardItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.BoardId == boardId, ct);

        if (item is null)
            return Result<long>.Failure("Item not found", ResultCategories.NotFound);

        if (!Enum.TryParse<BoardItemStatus>(request.Status, ignoreCase: true, out var status))
            return Result<long>.Failure("Status must be 'Needed', 'Completed', or 'Removed'", ResultCategories.Validation);

        if (item.Status == status)
            return Result<long>.Success(item.Id);

        item.Status = status;
        item.UpdatedAt = DateTime.UtcNow;

        switch (status)
        {
            case BoardItemStatus.Completed:
                item.CompletedByUserId = currentUser.UserId;
                item.RemovedAt = null;
                break;
            case BoardItemStatus.Needed:
                item.CompletedByUserId = null;
                item.RemovedAt = null;
                break;
            case BoardItemStatus.Removed:
                item.RemovedAt = DateTime.UtcNow;
                break;
        }

        await db.SaveChangesAsync(ct);

        broadcaster.Publish(boardId, new BoardEvent(BoardEventNames.ItemStatus, new { itemId = item.Id, status = status.ToString() }));

        return Result<long>.Success(item.Id);
    }
}
