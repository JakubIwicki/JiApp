using JiApp.Scheduler.Domain;

namespace JiApp.Scheduler.Features.Clients.GetClient;

[Serializable]
public sealed record GetClientResponse(
    long Id,
    string Name,
    string? Phone,
    string? Notes,
    List<AppointmentSummary> Appointments);

[Serializable]
public sealed record AppointmentSummary(
    long Id,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string ServiceName,
    string Status);
