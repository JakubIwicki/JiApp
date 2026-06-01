namespace JiApp.Scheduler.Features.DayTotals;

[Serializable]
public sealed record DayTotalsRequest(long BoardId, DateOnly Date);
