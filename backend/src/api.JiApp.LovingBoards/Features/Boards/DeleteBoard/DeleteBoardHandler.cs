using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using api.JiApp.LovingBoards.Features.Common;
using api.JiApp.LovingBoards.Persistence;

namespace api.JiApp.LovingBoards.Features.Boards.DeleteBoard;

public sealed class DeleteBoardHandler(ILovingBoardsDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<long>> HandleAsync(long id, CancellationToken ct)
    {
        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, id, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<long>.Failure(boardResult.Error!, boardResult.ErrorCategory);
        var board = boardResult.Value!;

        db.Boards.Remove(board);
        await db.SaveChangesAsync(ct);
        return Result<long>.Success(id);
    }
}
