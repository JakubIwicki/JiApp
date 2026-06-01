namespace JiApp.Scheduler.Features.Reports.RevenueReport;

[Serializable]
public sealed record RevenueReportRequest(
    long BoardId,
    DateOnly From,
    DateOnly To,
    string GroupBy);