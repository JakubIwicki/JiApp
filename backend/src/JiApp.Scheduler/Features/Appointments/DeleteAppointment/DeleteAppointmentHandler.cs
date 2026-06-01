using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Domain;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;

namespace JiApp.Scheduler.Features.Appointments.DeleteAppointment;

public sealed class DeleteAppointmentHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<long>> HandleAsync(long id, CancellationToken ct)
    {
        var appointment = await db.Appointments.FindAsync([id], ct);
        if (appointment is null)
            return Result<long>.Failure("Appointment not found", ResultCategories.NotFound);

        if (appointment.Status == AppointmentStatus.Done)
            return Result<long>.Failure("Cannot delete a completed appointment", ResultCategories.Conflict);

        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, appointment.BoardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<long>.Failure(boardResult.Error!, boardResult.ErrorCategory);

        db.Appointments.Remove(appointment);
        await db.SaveChangesAsync(ct);
        return Result<long>.Success(id);
    }
}