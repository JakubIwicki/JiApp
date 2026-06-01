using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Domain;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;

namespace JiApp.Scheduler.Features.Appointments.UpdateAppointmentStatus;

public sealed class UpdateAppointmentStatusHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<long>> HandleAsync(long id, UpdateAppointmentStatusRequest request, CancellationToken ct)
    {
        var appointment = await db.Appointments.FindAsync([id], ct);
        if (appointment is null)
            return Result<long>.Failure("Appointment not found", ResultCategories.NotFound);

        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, appointment.BoardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<long>.Failure(boardResult.Error!, boardResult.ErrorCategory);

        var newStatus = request.Status.ToLowerInvariant() switch
        {
            "done" => AppointmentStatus.Done,
            "cancel" or "cancelled" => AppointmentStatus.Cancelled,
            _ => (AppointmentStatus?)null
        };

        if (newStatus is null)
            return Result<long>.Failure("Invalid status. Use 'done', 'cancel', or 'cancelled'", ResultCategories.Validation);

        if (!appointment.TryTransitionTo(newStatus.Value, out var error))
            return Result<long>.Failure(error!, ResultCategories.Validation);

        await db.SaveChangesAsync(ct);
        return Result<long>.Success(appointment.Id);
    }
}