using JiApp.Common.Models;

namespace JiApp.Scheduler.Domain;

public sealed class Client : BaseEntity<long>
{
    public long BoardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public List<Appointment> Appointments { get; set; } = [];
    public Board Board { get; set; } = null!;
}