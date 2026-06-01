namespace JiApp.Scheduler.Features.Reports.ClientReport;

[Serializable]
public sealed record ClientReportResponse(
    long ClientId,
    string ClientName,
    int VisitCount,
    decimal TotalSpent,
    DateOnly? LastVisitDate,
    decimal AveragePerVisit);