using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Scheduler.Features.Clients.DeleteClient;

public sealed class DeleteClientHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<long>> HandleAsync(long id, CancellationToken ct)
    {
        var client = await db.Clients.FindAsync([id], ct);
        if (client is null)
            return Result<long>.Failure("Client not found", ResultCategories.NotFound);

        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, client.BoardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<long>.Failure(boardResult.Error!, boardResult.ErrorCategory);

        var hasAppointments = await db.Appointments.AnyAsync(a => a.ClientId == id, ct);
        if (hasAppointments)
            return Result<long>.Failure("Cannot delete client with existing appointments", ResultCategories.Conflict);

        db.Clients.Remove(client);
        await db.SaveChangesAsync(ct);
        return Result<long>.Success(id);
    }
}