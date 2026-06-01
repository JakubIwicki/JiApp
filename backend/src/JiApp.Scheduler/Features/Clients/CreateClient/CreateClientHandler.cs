using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Domain;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;

namespace JiApp.Scheduler.Features.Clients.CreateClient;

public sealed class CreateClientHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<long>> HandleAsync(CreateClientRequest request, CancellationToken ct)
    {
        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, request.BoardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<long>.Failure(boardResult.Error!, boardResult.ErrorCategory);

        var client = new Client
        {
            BoardId = request.BoardId,
            Name = request.Name,
            Phone = request.Phone,
            Notes = request.Notes,
        };
        db.Clients.Add(client);
        await db.SaveChangesAsync(ct);
        return Result<long>.Success(client.Id);
    }
}
