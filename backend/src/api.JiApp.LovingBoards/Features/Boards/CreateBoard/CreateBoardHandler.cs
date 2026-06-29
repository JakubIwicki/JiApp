using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using api.JiApp.LovingBoards.Configuration;
using api.JiApp.LovingBoards.Domain;
using api.JiApp.LovingBoards.Persistence;
using Microsoft.EntityFrameworkCore;

namespace api.JiApp.LovingBoards.Features.Boards.CreateBoard;

public sealed class CreateBoardHandler(
    ILovingBoardsDbContext db,
    LovingBoardsSettings settings,
    ICurrentUserService currentUser)
{
    public async Task<Result<long>> HandleAsync(CreateBoardRequest request, CancellationToken ct)
    {
        var userBoardCount = await db.Boards
            .CountAsync(b => b.OwnerUserId == currentUser.UserId, ct);

        if (userBoardCount >= settings.MaxBoardsPerUser)
            return Result<long>.Failure(
                $"Maximum number of boards ({settings.MaxBoardsPerUser}) reached",
                ResultCategories.Validation);

        var board = new Board
        {
            Name = request.Name,
            OwnerUserId = currentUser.UserId,
            MemberUserIds = [currentUser.UserId]
        };
        db.Boards.Add(board);
        await db.SaveChangesAsync(ct);
        return Result<long>.Success(board.Id);
    }
}
