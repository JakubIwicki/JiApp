using JiApp.Scheduler.Features.Clients;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Features.Services;

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
    DateTime CreatedAt,
    ClientResponse Client,
    ServiceResponse Service);