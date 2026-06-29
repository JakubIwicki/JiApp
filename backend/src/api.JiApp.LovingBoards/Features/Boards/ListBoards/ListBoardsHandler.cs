using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using api.JiApp.LovingBoards.Configuration;
using api.JiApp.LovingBoards.Domain;
using api.JiApp.LovingBoards.Features.Boards.GetBoard;
using api.JiApp.LovingBoards.Persistence;
using Microsoft.EntityFrameworkCore;

namespace api.JiApp.LovingBoards.Features.Boards.ListBoards;

public sealed class ListBoardsHandler(
    ILovingBoardsDbContext db,
    LovingBoardsSettings settings,
    ICurrentUserService currentUser)
{
    public async Task<Result<ListBoardsResponse>> HandleAsync(CancellationToken ct)
    {
        var boards = await db.Boards
            .AsNoTracking()
            .ToListAsync(ct);

        var userBoards = boards
            .Where(b => b.MemberUserIds.Contains(currentUser.UserId))
            .OrderBy(b => b.CreatedAt)
            .Select(b => new GetBoardResponse(b.Id, b.Name, b.OwnerUserId, b.MemberUserIds, b.CreatedAt))
            .ToList();

        // Seeding: if user has zero boards, create defaults
        if (userBoards.Count == 0)
        {
            var defaults = new[]
            {
                new Board { Name = "Groceries", OwnerUserId = currentUser.UserId, MemberUserIds = [currentUser.UserId] },
                new Board { Name = "Home", OwnerUserId = currentUser.UserId, MemberUserIds = [currentUser.UserId] }
            };

            foreach (var board in defaults)
                db.Boards.Add(board);

            await db.SaveChangesAsync(ct);

            userBoards = defaults
                .Select(b => new GetBoardResponse(b.Id, b.Name, b.OwnerUserId, b.MemberUserIds, b.CreatedAt))
                .ToList();
        }

        var totalCount = boards.Count(b => b.MemberUserIds.Contains(currentUser.UserId));
        var hasMore = totalCount > settings.DefaultPageSize;

        return Result<ListBoardsResponse>.Success(new ListBoardsResponse(userBoards, hasMore));
    }
}
