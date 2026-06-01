using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Scheduler.Features.Services.DeleteService;

public sealed class DeleteServiceHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<long>> HandleAsync(long id, CancellationToken ct)
    {
        var service = await db.Services.FindAsync([id], ct);
        if (service is null)
            return Result<long>.Failure("Service not found", ResultCategories.NotFound);

        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, service.BoardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<long>.Failure(boardResult.Error!, boardResult.ErrorCategory);

        var hasAppointments = await db.Appointments.AnyAsync(a => a.ServiceId == id, ct);
        if (hasAppointments)
            return Result<long>.Failure("Cannot delete service used in appointments", ResultCategories.Conflict);

        db.Services.Remove(service);
        await db.SaveChangesAsync(ct);
        return Result<long>.Success(id);
    }
}