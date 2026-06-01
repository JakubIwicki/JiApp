using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;

namespace JiApp.Scheduler.Features.Clients.UpdateClient;

public sealed class UpdateClientHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<long>> HandleAsync(long id, UpdateClientRequest request, CancellationToken ct)
    {
        var client = await db.Clients.FindAsync([id], ct);
        if (client is null)
            return Result<long>.Failure("Client not found", ResultCategories.NotFound);

        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, client.BoardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<long>.Failure(boardResult.Error!, boardResult.ErrorCategory);

        client.Name = request.Name;
        client.Phone = request.Phone;
        client.Notes = request.Notes;
        await db.SaveChangesAsync(ct);
        return Result<long>.Success(client.Id);
    }
}