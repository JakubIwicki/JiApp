using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Domain;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Scheduler.Features.Appointments.CreateAppointment;

public sealed class CreateAppointmentHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
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

        const int maxRetries = 1;
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                await using var tx =
                    await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

                if (await AppointmentHelpers.HasOverlapAsync(db, request.BoardId, request.Date, request.StartTime,
                        request.EndTime, null, ct))
                    return Result<long>.Failure("Appointment time overlaps with an existing appointment",
                        ResultCategories.Conflict);

                var appointment = BuildAppointment(request, price, currentUser.UserId);
                db.Appointments.Add(appointment);
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return Result<long>.Success(appointment.Id);
            }
            catch (DbUpdateException) when (attempt < maxRetries)
            {
                // Serialization conflict — retry once
            }
        }

        return Result<long>.Failure("Could not create appointment due to a concurrent update. Please try again.",
            ResultCategories.Conflict);
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