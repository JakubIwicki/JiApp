// ReSharper disable PropertyCanBeMadeInitOnly.Global

using System.ComponentModel.DataAnnotations;

namespace JiApp.Common.Models;

public enum EventLogType
{
    Exception = 0,
    ThirdPartyService = 1,
    Insider = 2
}

public class EventLog : BaseEntity<long>
{
    public EventLogType Type { get; set; }
    public long? UserId { get; set; }
    public DateTime? Timestamp { get; set; }

    [MaxLength(50000)] public string? Message { get; set; }

    [MaxLength(20000)] public string? Exception { get; set; }

    public static EventLog Create(EventLogType type, long? userId, string message)
    {
        return new EventLog
        {
            Type = type,
            UserId = userId,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }
}