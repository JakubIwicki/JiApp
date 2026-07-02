using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;

namespace JiApp.Scheduler.Features.Boards.DeleteBoard;

public sealed class DeleteBoardHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<long>> HandleAsync(long id, CancellationToken ct)
    {
        var boardResult = await BoardAccessGuard.VerifyBoardOwnerAsync(db, id, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<long>.Failure(boardResult.Error!, boardResult.ErrorCategory);
        var board = boardResult.Value!;

        db.Boards.Remove(board);
        await db.SaveChangesAsync(ct);
        return Result<long>.Success(id);
    }
}