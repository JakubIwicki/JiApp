using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Clients;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Features.Services;
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

        var client = await db.Clients.FindAsync([appointment.ClientId], ct);
        var service = await db.Services.FindAsync([appointment.ServiceId], ct);
        if (client is null || service is null)
            return Result<AppointmentResponse>.Failure("Appointment not found", ResultCategories.NotFound);

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
            appointment.CreatedAt,
            new ClientResponse(client.Id, client.BoardId, client.Name, client.Phone, client.Notes),
            new ServiceResponse(service.Id, service.BoardId, service.Name, service.Category.ToString(),
                service.BaseDuration, new PriceResponse(service.BasePrice.Amount, service.BasePrice.Currency)));

        return Result<AppointmentResponse>.Success(response);
    }
}