using JiApp.Common.Abstractions;
using JiApp.Common.Resilience;
using JiApp.Common.Services;
using JiApp.Scheduler.Domain;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Scheduler.Features.Appointments.CreateAppointment;

public sealed class CreateAppointmentHandler(
    ISchedulerDbContext db,
    ICurrentUserService currentUser,
    IRetryPolicyFactory retryPolicy)
{
    public async Task<Result<long>> HandleAsync(CreateAppointmentRequest request, CancellationToken ct)
    {
        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, request.BoardId, currentUser, ct);
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

                if (await AppointmentHelpers.HasOverlapAsync(db, request.BoardId, request.Date, request.StartTime,
                        request.EndTime, null, ctInner))
                    return Result<long>.Failure("Appointment time overlaps with an existing appointment",
                        ResultCategories.Conflict);

                var appointment = BuildAppointment(request, price, currentUser.UserId);
                db.Appointments.Add(appointment);
                await db.SaveChangesAsync(ctInner);
                await tx.CommitAsync(ctInner);
                return Result<long>.Success(appointment.Id);
            }, ct);
        }
        catch (DbUpdateException)
        {
            return Result<long>.Failure("Could not create appointment due to a concurrent update. Please try again.",
                ResultCategories.Conflict);
        }
    }

    private static Appointment BuildAppointment(CreateAppointmentRequest request, Price price, long userId) =>
        new()
        {
            BoardId = request.BoardId,
            ClientId = request.ClientId,
            ServiceId = request.ServiceId,
            Description = request.Description,
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Price = price,
            Location = request.Location,
            CreatedBy = userId
        };
}