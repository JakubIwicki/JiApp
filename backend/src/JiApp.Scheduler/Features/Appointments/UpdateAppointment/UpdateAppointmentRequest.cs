using JiApp.Scheduler.Features.Common;

namespace JiApp.Scheduler.Features.Appointments.UpdateAppointment;

[Serializable]
public sealed record UpdateAppointmentRequest(
    long ClientId,
    long ServiceId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Description,
    string Location,
    PriceRequest? Price) : IAppointmentRequest;