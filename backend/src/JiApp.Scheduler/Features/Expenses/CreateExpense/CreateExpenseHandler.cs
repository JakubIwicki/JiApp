using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using JiApp.Scheduler.Domain;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Persistence;

namespace JiApp.Scheduler.Features.Expenses.CreateExpense;

public sealed class CreateExpenseHandler(ISchedulerDbContext db, ICurrentUserService currentUser)
{
    public async Task<Result<long>> HandleAsync(CreateExpenseRequest request, CancellationToken ct)
    {
        var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, request.BoardId, currentUser, ct);
        if (!boardResult.IsSuccess)
            return Result<long>.Failure(boardResult.Error!, boardResult.ErrorCategory);

        if (!Enum.TryParse<ExpenseCategory>(request.Category, ignoreCase: true, out var category))
            return Result<long>.Failure($"Invalid expense category: {request.Category}", ResultCategories.Validation);

        var expense = new Expense
        {
            BoardId = request.BoardId,
            Date = request.Date,
            Category = category,
            Amount = new Price(request.Amount.Amount, request.Amount.Currency),
            Note = request.Note
        };

        db.Expenses.Add(expense);
        await db.SaveChangesAsync(ct);
        return Result<long>.Success(expense.Id);
    }
}