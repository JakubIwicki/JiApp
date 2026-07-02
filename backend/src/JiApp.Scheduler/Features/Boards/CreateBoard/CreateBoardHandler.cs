using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Domain;
using JiApp.Scheduler.Persistence;

namespace JiApp.Scheduler.Features.Boards.CreateBoard;

public sealed class CreateBoardHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<long>> HandleAsync(CreateBoardRequest request, CancellationToken ct)
    {
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
