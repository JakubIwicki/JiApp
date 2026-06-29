using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using api.JiApp.LovingBoards.Features.Common;
using api.JiApp.LovingBoards.Persistence;

namespace api.JiApp.LovingBoards.Features.Boards.AddBoardMember;

public sealed class AddBoardMemberHandler(ILovingBoardsDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<long>> HandleAsync(long boardId, AddBoardMemberRequest request, CancellationToken ct)
    {
        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, boardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<long>.Failure(boardResult.Error!, boardResult.ErrorCategory);
        var board = boardResult.Value!;

        if (board.MemberUserIds.Contains(request.UserId))
            return Result<long>.Failure("User is already a member of this board", ResultCategories.Conflict);

        board.MemberUserIds.Add(request.UserId);
        await db.SaveChangesAsync(ct);
        return Result<long>.Success(board.Id);
    }
}
