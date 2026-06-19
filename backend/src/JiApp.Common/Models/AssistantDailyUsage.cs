// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace JiApp.Common.Models;

public class AssistantDailyUsage : BaseEntity<long>
{
    public long UserId { get; set; }
    public DateOnly UsageDateUtc { get; set; }
    public int Count { get; set; }
}
