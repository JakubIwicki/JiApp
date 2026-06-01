using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;

namespace JiApp.Scheduler.Features.Appointments.GetAppointment;

public sealed class GetAppointmentHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<AppointmentResponse>> HandleAsync(long id, CancellationToken ct)
    {
        var appointment = await db.Appointments.FindAsync([id], ct);
        if (appointment is null)
            return Result<AppointmentResponse>.Failure("Appointment not found", ResultCategories.NotFound);

        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, appointment.BoardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<AppointmentResponse>.Failure(boardResult.Error!, boardResult.ErrorCategory);

        var response = new AppointmentResponse(
            appointment.Id,
            appointment.BoardId,
            appointment.ClientId,
            appointment.ServiceId,
            appointment.Description,
            appointment.Date,
            appointment.StartTime,
            appointment.EndTime,
            new PriceResponse(appointment.Price.Amount, appointment.Price.Currency),
            appointment.Location,
            appointment.Status.ToString(),
            appointment.CreatedAt);

        return Result<AppointmentResponse>.Success(response);
    }
}