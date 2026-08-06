using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using api.JiApp.LovingBoards.Clients;
using api.JiApp.LovingBoards.Common;
using api.JiApp.LovingBoards.Configuration;
using api.JiApp.LovingBoards.Features.Common;
using api.JiApp.LovingBoards.Persistence;
using api.JiApp.LovingBoards.Realtime;

namespace api.JiApp.LovingBoards.Features.Boards.AddBoardMember;

public sealed class AddBoardMemberHandler(
    ILovingBoardsDbContext db,
    ICurrentUserService currentUser,
    IBoardBroadcaster broadcaster,
    BoardWriteLock boardLock,
    LovingBoardsSettings settings,
    IUserExistenceClient userExistenceClient)
{
    public async Task<Result<long>> HandleAsync(long boardId, AddBoardMemberRequest request, CancellationToken ct)
    {
        // The existence probe is an outbound Identity call (up to the client's 5s timeout)
        // that reads no board state — run it before the board write lock so a slow or failed
        // probe cannot stall every board write for the whole timeout.
        var existence = await userExistenceClient.CheckExistsAsync(request.UserId, ct);
        if (existence == UserExistenceStatus.NotFound)
            return Result<long>.Failure("User not found", ResultCategories.NotFound);

        // Fail closed: without a confirmed existence verdict we must not add the member.
        if (existence == UserExistenceStatus.Unavailable)
            return Result<long>.Failure(
                "Unable to verify user existence; try again later", ResultCategories.Unavailable);

        using var _ = await boardLock.AcquireAsync(boardId, ct);

        var boardResult = await BoardAccessGuard.VerifyBoardOwnerAsync(db, boardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<long>.Failure(boardResult.Error!, boardResult.ErrorCategory);
        var board = boardResult.Value!;

        if (board.MemberUserIds.Contains(request.UserId))
            return Result<long>.Failure("User is already a member of this board", ResultCategories.Conflict);

        if (board.MemberUserIds.Count >= settings.MaxMembersPerBoard)
            return Result<long>.Failure(
                $"Maximum number of members ({settings.MaxMembersPerBoard}) reached for this board",
                ResultCategories.Validation);

        board.MemberUserIds.Add(request.UserId);
        await db.SaveChangesAsync(ct);

        broadcaster.Publish(boardId, new BoardEvent(BoardEventNames.MemberChanged, new { boardId }));

        return Result<long>.Success(board.Id);
    }
}
