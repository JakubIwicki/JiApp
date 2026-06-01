namespace JiApp.Scheduler.Features.Common;

public interface IAppointmentRequest
{
    long ClientId { get; }
    long ServiceId { get; }
    DateOnly Date { get; }
    TimeOnly StartTime { get; }
    TimeOnly EndTime { get; }
    string? Description { get; }
    string Location { get; }
}