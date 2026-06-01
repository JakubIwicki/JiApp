using JiApp.Scheduler.Features.Common;

namespace JiApp.Scheduler.Features.Appointments.CreateAppointment;

[Serializable]
public sealed record CreateAppointmentRequest(
    long BoardId,
    long ClientId,
    long ServiceId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Description,
    string Location,
    PriceRequest? Price) : IAppointmentRequest;