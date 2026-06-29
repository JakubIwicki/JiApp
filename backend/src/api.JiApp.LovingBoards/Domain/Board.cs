using JiApp.Common.Models;

namespace api.JiApp.LovingBoards.Domain;

public sealed class Board : BaseEntity<long>
{
    public string Name { get; set; } = string.Empty;
    public long OwnerUserId { get; set; }
    public List<long> MemberUserIds { get; set; } = [];
    public DateTime? LastWeeklyResetAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
