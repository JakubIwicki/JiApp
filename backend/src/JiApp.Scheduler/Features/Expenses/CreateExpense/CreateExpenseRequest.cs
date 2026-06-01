using JiApp.Scheduler.Features.Common;

namespace JiApp.Scheduler.Features.Expenses.CreateExpense;

[Serializable]
public sealed record CreateExpenseRequest(
    long BoardId,
    DateOnly Date,
    string Category,
    PriceRequest Amount,
    string? Note);