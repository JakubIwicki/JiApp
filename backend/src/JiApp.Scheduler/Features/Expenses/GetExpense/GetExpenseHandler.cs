using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;

namespace JiApp.Scheduler.Features.Expenses.GetExpense;

public sealed class GetExpenseHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<ExpenseResponse>> HandleAsync(long id, CancellationToken ct)
    {
        var expense = await db.Expenses.FindAsync([id], ct);
        if (expense is null)
            return Result<ExpenseResponse>.Failure("Expense not found", ResultCategories.NotFound);

        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, expense.BoardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<ExpenseResponse>.Failure(boardResult.Error!, boardResult.ErrorCategory);

        var response = new ExpenseResponse(
            expense.Id,
            expense.BoardId,
            expense.Date,
            expense.Category.ToString(),
            expense.Amount.Amount,
            expense.Amount.Currency,
            expense.Note);

        return Result<ExpenseResponse>.Success(response);
    }
}