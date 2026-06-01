using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Domain;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Scheduler.Features.Reports.ClientReport;

public sealed class ClientReportHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<List<ClientReportResponse>>> HandleAsync(ClientReportRequest request, CancellationToken ct)
    {
        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, request.BoardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<List<ClientReportResponse>>.Failure(boardResult.Error!, boardResult.ErrorCategory);
        var sortBy = request.SortBy.ToLowerInvariant();

        var clientStats = await db.Clients
            .Where(c => c.Appointments.Any(a => a.BoardId == request.BoardId))
            .AsNoTracking()
            .Select(c => new
            {
                c.Id,
                c.Name,
                VisitCount = c.Appointments
                    .Where(a => a.BoardId == request.BoardId && a.Status != AppointmentStatus.Cancelled)
                    .Count(),
                TotalSpent = c.Appointments
                    .Where(a => a.BoardId == request.BoardId && a.Status != AppointmentStatus.Cancelled)
                    .Sum(a => (decimal?)a.Price.Amount) ?? 0,
                LastVisitDate = c.Appointments
                    .Where(a => a.BoardId == request.BoardId && a.Status != AppointmentStatus.Cancelled)
                    .Max(a => (DateOnly?)a.Date)
            })
            .ToListAsync(ct);

        var reportData = clientStats
            .Select(c => new ClientReportResponse(
                c.Id,
                c.Name,
                c.VisitCount,
                c.TotalSpent,
                c.LastVisitDate,
                c.VisitCount > 0 ? Math.Round(c.TotalSpent / c.VisitCount, 2) : 0))
            .ToList();

        reportData = sortBy switch
        {
            "frequency" => [.. reportData.OrderByDescending(r => r.VisitCount)],
            "totalSpent" => [.. reportData.OrderByDescending(r => r.TotalSpent)],
            "lastVisit" => [.. reportData.OrderByDescending(r => r.LastVisitDate)],
            _ => reportData
        };

        return Result<List<ClientReportResponse>>.Success(reportData);
    }
}