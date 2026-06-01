using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Domain;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Scheduler.Features.DayTotals;

public sealed class DayTotalsHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<DayTotalsResponse>> HandleAsync(DayTotalsRequest request, CancellationToken ct)
    {
        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, request.BoardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<DayTotalsResponse>.Failure(boardResult.Error!, boardResult.ErrorCategory);
        var revenue = await db.Appointments
            .Where(a => a.BoardId == request.BoardId && a.Date == request.Date && a.Status != AppointmentStatus.Cancelled)
            .SumAsync(a => a.Price.Amount, ct);

        var expenses = await db.Expenses
            .Where(e => e.BoardId == request.BoardId && e.Date == request.Date)
            .SumAsync(e => e.Amount.Amount, ct);

        return Result<DayTotalsResponse>.Success(new DayTotalsResponse(
            revenue,
            expenses,
            revenue - expenses));
    }
}