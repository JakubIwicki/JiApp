namespace api.JiApp.LovingBoards.Features.Items.CreateItem;

[Serializable]
public sealed record CreateItemRequest(
    string Title,
    string? Quantity = null,
    string? Category = null,
    string? Note = null,
    long? AssigneeUserId = null,
    DateTime? ExpiryDate = null,
    bool IsRecurring = false);
