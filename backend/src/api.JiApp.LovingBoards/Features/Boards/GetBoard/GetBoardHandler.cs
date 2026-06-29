using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using api.JiApp.LovingBoards.Domain;
using api.JiApp.LovingBoards.Features.Common;
using api.JiApp.LovingBoards.Persistence;
using api.JiApp.LovingBoards.Realtime;
using Microsoft.EntityFrameworkCore;

namespace api.JiApp.LovingBoards.Features.Boards.GetBoard;

public sealed class GetBoardHandler(ILovingBoardsDbContext db, ICurrentUserService currentUser, IBoardBroadcaster broadcaster)
{
    public async Task<Result<GetBoardResponse>> HandleAsync(long id, CancellationToken ct)
    {
        var board = await db.Boards.FindAsync([id], ct);

        if (board is null)
            return Result<GetBoardResponse>.Failure("Board not found", ResultCategories.NotFound);

        if (!board.MemberUserIds.Contains(currentUser.UserId))
            return Result<GetBoardResponse>.Failure("Access denied", ResultCategories.AccessDenied);

        var items = await db.BoardItems
            .Where(i => i.BoardId == id)
            .OrderBy(i => i.Category)
            .ThenBy(i => i.CreatedAt)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        if (WeeklyReset.IsResetDue(board.LastWeeklyResetAt, now))
        {
            var resetCount = WeeklyReset.ResetRecurring(items, now);
            board.LastWeeklyResetAt = now;
            await db.SaveChangesAsync(ct);

            broadcaster.Publish(id, new BoardEvent(BoardEventNames.RecurringReset, new { reset = resetCount }));
        }

        var itemDtos = items
            .Where(i => i.Status != BoardItemStatus.Removed)
            .Select(i => new ItemDto(
                i.Id,
                i.BoardId,
                i.Title,
                i.Quantity,
                i.Category,
                i.Note,
                i.AssigneeUserId,
                i.ExpiryDate,
                i.IsRecurring,
                i.Status.ToString(),
                i.AddedByUserId,
                i.CompletedByUserId,
                i.CreatedAt,
                i.UpdatedAt,
                i.RemovedAt))
            .ToList();

        var response = new GetBoardResponse(
            board.Id,
            board.Name,
            board.OwnerUserId,
            board.MemberUserIds,
            board.CreatedAt,
            itemDtos);

        return Result<GetBoardResponse>.Success(response);
    }
}
