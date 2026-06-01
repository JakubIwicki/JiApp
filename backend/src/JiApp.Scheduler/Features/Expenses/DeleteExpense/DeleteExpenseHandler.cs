using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;

namespace JiApp.Scheduler.Features.Expenses.DeleteExpense;

public sealed class DeleteExpenseHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<long>> HandleAsync(long id, CancellationToken ct)
    {
        var expense = await db.Expenses.FindAsync([id], ct);
        if (expense is null)
            return Result<long>.Failure("Expense not found", ResultCategories.NotFound);

        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, expense.BoardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<long>.Failure(boardResult.Error!, boardResult.ErrorCategory);

        db.Expenses.Remove(expense);
        await db.SaveChangesAsync(ct);
        return Result<long>.Success(id);
    }
}