namespace JiApp.Scheduler.Features.Reports.RevenueReport;

[Serializable]
public sealed record RevenueReportResponse(
    string GroupKey,
    decimal Revenue,
    decimal Expenses,
    decimal Net,
    int AppointmentCount);