using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Domain;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Scheduler.Features.Reports.RevenueReport;

public sealed class RevenueReportHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<List<RevenueReportResponse>>> HandleAsync(RevenueReportRequest request,
        CancellationToken ct)
    {
        if (request.From > request.To)
            return Result<List<RevenueReportResponse>>.Failure("From date must be before or equal to To date", ResultCategories.Validation);

        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, request.BoardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<List<RevenueReportResponse>>.Failure(boardResult.Error!, boardResult.ErrorCategory);

        var groupBy = request.GroupBy.ToLowerInvariant();

        var appointments = await db.Appointments
            .AsNoTracking()
            .Where(a => a.BoardId == request.BoardId
                        && a.Date >= request.From
                        && a.Date <= request.To
                        && a.Status != AppointmentStatus.Cancelled)
            .Include(a => a.Service)
            .Include(a => a.Client)
            .ToListAsync(ct);

        var expenses = await db.Expenses
            .AsNoTracking()
            .Where(e => e.BoardId == request.BoardId
                        && e.Date >= request.From
                        && e.Date <= request.To)
            .ToListAsync(ct);

        var grouped = groupBy switch
        {
            "weekend" => GroupByWeekend(appointments, expenses),
            "service" => GroupByKey(appointments, expenses, a => a.Service.Name),
            "location" => GroupByKey(appointments, expenses, a => a.Location),
            "client" => GroupByKey(appointments, expenses, a => a.Client.Name),
            _ => null
        };

        if (grouped is null)
            return Result<List<RevenueReportResponse>>.Failure(
                "Invalid groupBy. Use: weekend, service, location, or client", ResultCategories.Validation);

        return Result<List<RevenueReportResponse>>.Success(grouped);
    }

    private static List<RevenueReportResponse> GroupByWeekend(
        List<Appointment> appointments, List<Expense> expenses)
    {
        // Group all appointments and expenses into Sat-Sun pairs
        var weekendGroups = new Dictionary<string, (decimal Revenue, decimal Expenses, int Count)>();

        foreach (var appt in appointments)
        {
            var key = GetWeekendGroupKey(appt.Date);
            var current = weekendGroups.GetValueOrDefault(key);
            current.Revenue += appt.Price.Amount;
            current.Count++;
            weekendGroups[key] = current;
        }

        foreach (var exp in expenses)
        {
            var key = GetWeekendGroupKey(exp.Date);
            var current = weekendGroups.GetValueOrDefault(key);
            current.Expenses += exp.Amount.Amount;
            weekendGroups[key] = current;
        }

        return weekendGroups
            .Select(kv => new RevenueReportResponse(
                kv.Key, kv.Value.Revenue, kv.Value.Expenses,
                kv.Value.Revenue - kv.Value.Expenses, kv.Value.Count))
            .OrderBy(r => r.GroupKey)
            .ToList();
    }

    private static string GetWeekendGroupKey(DateOnly date)
    {
        // Find the Saturday of the weekend containing this date
        var saturday = date.DayOfWeek == DayOfWeek.Sunday ? date.AddDays(-1) : date;
        return $"{saturday:yyyy dd MMM}";
    }

    private static List<RevenueReportResponse> GroupByKey(
        List<Appointment> appointments, List<Expense> expenses,
        Func<Appointment, string> keySelector)
    {
        var totalRevenue = appointments.Sum(a => a.Price.Amount);
        var totalExpenses = expenses.Sum(e => e.Amount.Amount);

        return appointments
            .GroupBy(keySelector)
            .Select(g =>
            {
                var revenue = g.Sum(a => a.Price.Amount);
                var allocatedExpenses = DistributeExpenses(revenue, totalRevenue, totalExpenses);
                return new RevenueReportResponse(
                    g.Key, revenue, allocatedExpenses,
                    revenue - allocatedExpenses, g.Count());
            })
            .OrderByDescending(r => r.Revenue)
            .ToList();
    }

    private static decimal DistributeExpenses(decimal groupRevenue, decimal totalRevenue, decimal totalExpenses)
    {
        if (totalRevenue == 0 || totalExpenses == 0)
            return 0;

        return Math.Round(groupRevenue / totalRevenue * totalExpenses, 2);
    }
}