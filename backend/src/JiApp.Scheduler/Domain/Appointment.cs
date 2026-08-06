using JiApp.Common.Abstractions;
using JiApp.Common.Models;

namespace JiApp.Scheduler.Domain;

public enum AppointmentStatus
{
    Created,
    Done,
    Cancelled
}

public sealed class Appointment : BaseEntity<long>
{
    public long BoardId { get; set; }
    public long ClientId { get; set; }
    public long ServiceId { get; set; }
    public string? Description { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public Price Price { get; set; } = new();
    public string Location { get; set; } = string.Empty;
    private AppointmentStatus _status = AppointmentStatus.Created;
    public AppointmentStatus Status
    {
        get => _status;
        private set => _status = value;
    }
    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }

    public Board Board { get; set; } = null!;
    public Client Client { get; set; } = null!;
    public Service Service { get; set; } = null!;

    public Result TryTransitionTo(AppointmentStatus newStatus)
    {
        if (Status == AppointmentStatus.Created)
        {
            if (newStatus is AppointmentStatus.Done or AppointmentStatus.Cancelled)
            {
                Status = newStatus;
                return Result.Success();
            }
        }

        return Result.Failure($"Cannot change status from {Status} to {newStatus}");
    }
}