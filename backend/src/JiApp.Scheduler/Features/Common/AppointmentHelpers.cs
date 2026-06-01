using JiApp.Scheduler.Domain;
using JiApp.Scheduler.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Scheduler.Features.Common;

internal static class AppointmentHelpers
{
    internal static async Task<bool> ClientExistsAsync(ISchedulerDbContext db, long clientId, CancellationToken ct) =>
        await db.Clients.AnyAsync(c => c.Id == clientId, ct);

    internal static async Task<Service?>
        FindServiceAsync(ISchedulerDbContext db, long serviceId, CancellationToken ct) =>
        await db.Services.FirstOrDefaultAsync(s => s.Id == serviceId, ct);

    internal static async Task<bool> HasOverlapAsync(
        ISchedulerDbContext db, long boardId, DateOnly date, TimeOnly startTime, TimeOnly endTime,
        long? excludeAppointmentId, CancellationToken ct)
    {
        var query = db.Appointments
            .Where(a => a.BoardId == boardId && a.Date == date && a.StartTime < endTime && a.EndTime > startTime);

        if (excludeAppointmentId.HasValue)
            query = query.Where(a => a.Id != excludeAppointmentId.Value);

        return await query.AnyAsync(ct);
    }
}