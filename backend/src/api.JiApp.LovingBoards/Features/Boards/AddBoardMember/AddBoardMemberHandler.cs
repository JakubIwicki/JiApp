using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using api.JiApp.LovingBoards.Common;
using api.JiApp.LovingBoards.Features.Common;
using api.JiApp.LovingBoards.Persistence;
using api.JiApp.LovingBoards.Realtime;

namespace api.JiApp.LovingBoards.Features.Boards.AddBoardMember;

public sealed class AddBoardMemberHandler(
    ILovingBoardsDbContext db,
    ICurrentUserService currentUser,
    IBoardBroadcaster broadcaster,
    BoardWriteLock boardLock)
{
    public async Task<Result<long>> HandleAsync(long boardId, AddBoardMemberRequest request, CancellationToken ct)
    {
        using var _ = await boardLock.AcquireAsync(boardId, ct);

        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, boardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<long>.Failure(boardResult.Error!, boardResult.ErrorCategory);
        var board = boardResult.Value!;

        if (board.MemberUserIds.Contains(request.UserId))
            return Result<long>.Failure("User is already a member of this board", ResultCategories.Conflict);

        board.MemberUserIds.Add(request.UserId);
        await db.SaveChangesAsync(ct);

        broadcaster.Publish(boardId, new BoardEvent(BoardEventNames.MemberChanged, new { boardId }));

        return Result<long>.Success(board.Id);
    }
}
