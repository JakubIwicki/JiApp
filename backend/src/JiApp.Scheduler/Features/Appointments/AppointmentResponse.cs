using JiApp.Scheduler.Features.Common;

namespace JiApp.Scheduler.Features.Appointments;

[Serializable]
public sealed record AppointmentResponse(
    long Id,
    long BoardId,
    long ClientId,
    long ServiceId,
    string? Description,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    PriceResponse Price,
    string Location,
    string Status,
    DateTime CreatedAt);