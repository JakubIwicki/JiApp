namespace JiApp.Scheduler.Features.Reports.ClientReport;

[Serializable]
public sealed record ClientReportRequest(
    long BoardId,
    string SortBy);