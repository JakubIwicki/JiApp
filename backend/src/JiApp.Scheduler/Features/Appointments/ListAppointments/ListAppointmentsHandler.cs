using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Scheduler.Features.Appointments.ListAppointments;

public sealed class ListAppointmentsHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<List<AppointmentResponse>>> HandleAsync(long boardId, DateOnly[]? dates,
        CancellationToken ct)
    {
        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, boardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<List<AppointmentResponse>>.Failure(boardResult.Error!, boardResult.ErrorCategory);
        var query = db.Appointments
            .Where(a => a.BoardId == boardId);

        if (dates is { Length: > 0 })
            query = query.Where(a => dates.Contains(a.Date));

        var appointments = await query
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime)
            .Select(a => new AppointmentResponse(
                a.Id,
                a.BoardId,
                a.ClientId,
                a.ServiceId,
                a.Description,
                a.Date,
                a.StartTime,
                a.EndTime,
                new PriceResponse(a.Price.Amount, a.Price.Currency),
                a.Location,
                a.Status.ToString(),
                a.CreatedAt))
            .ToListAsync(ct);

        return Result<List<AppointmentResponse>>.Success(appointments);
    }
}