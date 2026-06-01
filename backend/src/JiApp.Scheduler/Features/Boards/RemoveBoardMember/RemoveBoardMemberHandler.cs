using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;

namespace JiApp.Scheduler.Features.Boards.RemoveBoardMember;

/// <summary>
/// Handles removing a member from a board.
/// NOTE: Authorization — any board member can delete other members.
/// There is no owner/creator concept. Adding full OwnerId is out of scope.
/// </summary>
public sealed class RemoveBoardMemberHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<long>> HandleAsync(long boardId, long userId, CancellationToken ct)
    {
        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, boardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<long>.Failure(boardResult.Error!, boardResult.ErrorCategory);
        var board = boardResult.Value!;

        if (!board.MemberUserIds.Contains(userId))
            return Result<long>.Failure("Member not found", ResultCategories.NotFound);

        if (board.MemberUserIds.Count == 1)
            return Result<long>.Failure("Cannot remove the last member", ResultCategories.Conflict);

        board.MemberUserIds.Remove(userId);
        await db.SaveChangesAsync(ct);
        return Result<long>.Success(boardId);
    }
}