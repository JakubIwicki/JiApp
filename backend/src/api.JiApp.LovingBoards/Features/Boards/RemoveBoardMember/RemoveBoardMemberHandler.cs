using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using api.JiApp.LovingBoards.Features.Common;
using api.JiApp.LovingBoards.Persistence;
using api.JiApp.LovingBoards.Realtime;

namespace api.JiApp.LovingBoards.Features.Boards.RemoveBoardMember;

public sealed class RemoveBoardMemberHandler(ILovingBoardsDbContext db, ICurrentUserService currentUser, IBoardBroadcaster broadcaster)
{
    public async Task<Result<long>> HandleAsync(long boardId, long userId, CancellationToken ct)
    {
        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, boardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<long>.Failure(boardResult.Error!, boardResult.ErrorCategory);
        var board = boardResult.Value!;

        if (!board.MemberUserIds.Contains(userId))
            return Result<long>.Failure("Member not found", ResultCategories.NotFound);

        if (userId == board.OwnerUserId)
            return Result<long>.Failure("The board owner cannot be removed", ResultCategories.Conflict);

        if (board.MemberUserIds.Count == 1)
            return Result<long>.Failure("Cannot remove the last member", ResultCategories.Conflict);

        board.MemberUserIds.Remove(userId);
        await db.SaveChangesAsync(ct);

        broadcaster.Publish(boardId, new BoardEvent(BoardEventNames.MemberChanged, new { boardId }));
        broadcaster.Disconnect(boardId, userId);

        return Result<long>.Success(boardId);
    }
}
