using JiApp.Common.Abstractions;
using JiApp.Common.Resilience;
using JiApp.Common.Services;
using JiApp.Scheduler.Domain;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Scheduler.Features.Appointments.UpdateAppointment;

public sealed class UpdateAppointmentHandler(
    ISchedulerDbContext db,
    ICurrentUserService currentUser,
    IRetryPolicyFactory retryPolicy)
{
    public async Task<Result<long>> HandleAsync(long id, UpdateAppointmentRequest request, CancellationToken ct)
    {
        var appointment = await FindAppointmentAsync(id, ct);
        if (appointment is null)
            return Result<long>.Failure("Appointment not found", ResultCategories.NotFound);

        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, appointment.BoardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<long>.Failure(boardResult.Error!, boardResult.ErrorCategory);

        if (!await AppointmentHelpers.ClientExistsAsync(db, request.ClientId, ct))
            return Result<long>.Failure("Client not found", ResultCategories.NotFound);

        var service = await AppointmentHelpers.FindServiceAsync(db, request.ServiceId, ct);
        if (service is null)
            return Result<long>.Failure("Service not found", ResultCategories.NotFound);

        var price = AppointmentDomainHelpers.ResolvePrice(request.Price, service.BasePrice);

        var policy = retryPolicy.RetryOnDbConflict(retries: 1, delay: TimeSpan.Zero);

        try
        {
            return await policy.ExecuteAsync(async ctInner =>
            {
                await using var tx =
                    await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ctInner);

                // Re-load within transaction to get fresh state
                if (await AppointmentHelpers.HasOverlapAsync(db, appointment.BoardId, request.Date, request.StartTime,
                        request.EndTime, id, ctInner))
                    return Result<long>.Failure("Appointment time overlaps with an existing appointment",
                        ResultCategories.Conflict);

                appointment.Price = price;
                ApplyRequest(appointment, request);

                await db.SaveChangesAsync(ctInner);
                await tx.CommitAsync(ctInner);
                return Result<long>.Success(appointment.Id);
            }, ct);
        }
        catch (DbUpdateException)
        {
            return Result<long>.Failure("Could not update appointment due to a concurrent update. Please try again.",
                ResultCategories.Conflict);
        }
    }

    private async Task<Appointment?> FindAppointmentAsync(long id, CancellationToken ct)
        => await db.Appointments.FindAsync([id], ct);

    private static void ApplyRequest(Appointment appointment, UpdateAppointmentRequest request)
    {
        appointment.ClientId = request.ClientId;
        appointment.ServiceId = request.ServiceId;
        appointment.Description = request.Description;
        appointment.Date = request.Date;
        appointment.StartTime = request.StartTime;
        appointment.EndTime = request.EndTime;
        appointment.Location = request.Location;
    }
}