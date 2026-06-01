using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Scheduler.Features.Expenses.ListExpenses;

public sealed class ListExpensesHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<List<ExpenseResponse>>> HandleAsync(long boardId, DateOnly? date, CancellationToken ct)
    {
        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, boardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<List<ExpenseResponse>>.Failure(boardResult.Error!, boardResult.ErrorCategory);
        var query = db.Expenses
            .Where(e => e.BoardId == boardId);

        if (date.HasValue)
            query = query.Where(e => e.Date == date.Value);

        var expenses = await query
            .OrderBy(e => e.Date)
            .Select(e => new ExpenseResponse(
                e.Id,
                e.BoardId,
                e.Date,
                e.Category.ToString(),
                e.Amount.Amount,
                e.Amount.Currency,
                e.Note))
            .ToListAsync(ct);

        return Result<List<ExpenseResponse>>.Success(expenses);
    }
}