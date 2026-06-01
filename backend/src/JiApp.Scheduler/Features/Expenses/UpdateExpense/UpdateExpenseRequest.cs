using JiApp.Scheduler.Features.Common;

namespace JiApp.Scheduler.Features.Expenses.UpdateExpense;

[Serializable]
public sealed record UpdateExpenseRequest(
    DateOnly Date,
    string Category,
    PriceRequest Amount,
    string? Note);