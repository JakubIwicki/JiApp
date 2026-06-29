using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using api.JiApp.LovingBoards.Domain;
using api.JiApp.LovingBoards.Persistence;
using Microsoft.EntityFrameworkCore;

namespace api.JiApp.LovingBoards.Features.Boards.GetBoard;

public sealed class GetBoardHandler(ILovingBoardsDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<GetBoardResponse>> HandleAsync(long id, CancellationToken ct)
    {
        var board = await db.Boards.FindAsync([id], ct);

        if (board is null)
            return Result<GetBoardResponse>.Failure("Board not found", ResultCategories.NotFound);

        if (!board.MemberUserIds.Contains(currentUser.UserId))
            return Result<GetBoardResponse>.Failure("Access denied", ResultCategories.AccessDenied);

        var items = await db.BoardItems
            .Where(i => i.BoardId == id && i.Status != BoardItemStatus.Removed)
            .OrderBy(i => i.Category)
            .ThenBy(i => i.CreatedAt)
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
            .ToListAsync(ct);

        var response = new GetBoardResponse(
            board.Id,
            board.Name,
            board.OwnerUserId,
            board.MemberUserIds,
            board.CreatedAt,
            items);

        return Result<GetBoardResponse>.Success(response);
    }
}
