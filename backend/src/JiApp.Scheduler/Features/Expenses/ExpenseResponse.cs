namespace JiApp.Scheduler.Features.Expenses;

[Serializable]
public sealed record ExpenseResponse(
    long Id,
    long BoardId,
    DateOnly Date,
    string Category,
    decimal Amount,
    string Currency,
    string? Note);