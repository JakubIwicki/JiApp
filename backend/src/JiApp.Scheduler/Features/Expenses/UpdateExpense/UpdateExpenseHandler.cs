using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Domain;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;

namespace JiApp.Scheduler.Features.Expenses.UpdateExpense;

public sealed class UpdateExpenseHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<long>> HandleAsync(long id, UpdateExpenseRequest request, CancellationToken ct)
    {
        var expense = await db.Expenses.FindAsync([id], ct);
        if (expense is null)
            return Result<long>.Failure("Expense not found", ResultCategories.NotFound);

        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, expense.BoardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<long>.Failure(boardResult.Error!, boardResult.ErrorCategory);

        if (!Enum.TryParse<ExpenseCategory>(request.Category, true, out var category))
            return Result<long>.Failure("Invalid expense category", ResultCategories.Validation);

        expense.Date = request.Date;
        expense.Category = category;
        expense.Amount = new Price(request.Amount.Amount, request.Amount.Currency);
        expense.Note = request.Note;

        await db.SaveChangesAsync(ct);
        return Result<long>.Success(expense.Id);
    }
}