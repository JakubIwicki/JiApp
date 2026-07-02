using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using api.JiApp.LovingBoards.Features.Common;
using api.JiApp.LovingBoards.Persistence;
using api.JiApp.LovingBoards.Realtime;

namespace api.JiApp.LovingBoards.Features.Boards.DeleteBoard;

public sealed class DeleteBoardHandler(ILovingBoardsDbContext db, ICurrentUserService currentUser, IBoardBroadcaster broadcaster)
{
    public async Task<Result<long>> HandleAsync(long id, CancellationToken ct)
    {
        var boardResult = await BoardAccessGuard.VerifyBoardOwnerAsync(db, id, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<long>.Failure(boardResult.Error!, boardResult.ErrorCategory);
        var board = boardResult.Value!;

        db.Boards.Remove(board);
        await db.SaveChangesAsync(ct);

        broadcaster.Publish(id, new BoardEvent(BoardEventNames.BoardDeleted, new { boardId = id }));
        broadcaster.DisconnectAll(id);

        return Result<long>.Success(id);
    }
}
