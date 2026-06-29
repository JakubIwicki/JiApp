namespace api.JiApp.LovingBoards.Features.Boards.GetBoard;

[Serializable]
public sealed record ItemDto(
    long Id,
    long BoardId,
    string Title,
    string? Quantity,
    string? Category,
    string? Note,
    long? AssigneeUserId,
    DateTime? ExpiryDate,
    bool IsRecurring,
    string Status,
    long AddedByUserId,
    long? CompletedByUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? RemovedAt);
