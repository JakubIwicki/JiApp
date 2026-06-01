namespace JiApp.Scheduler.Features.DayTotals;

[Serializable]
public sealed record DayTotalsResponse(decimal Revenue, decimal Expenses, decimal Net);