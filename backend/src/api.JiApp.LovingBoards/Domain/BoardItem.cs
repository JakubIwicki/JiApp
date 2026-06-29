using JiApp.Common.Models;

namespace api.JiApp.LovingBoards.Domain;

public sealed class BoardItem : BaseEntity<long>
{
    public long BoardId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Quantity { get; set; }
    public string? Category { get; set; }
    public string? Note { get; set; }
    public long? AssigneeUserId { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsRecurring { get; set; }
    public BoardItemStatus Status { get; set; } = BoardItemStatus.Needed;
    public long AddedByUserId { get; set; }
    public long? CompletedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RemovedAt { get; set; }
}
