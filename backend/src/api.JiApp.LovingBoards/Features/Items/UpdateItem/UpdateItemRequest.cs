namespace api.JiApp.LovingBoards.Features.Items.UpdateItem;

[Serializable]
public sealed record UpdateItemRequest(
    string Title,
    string? Quantity = null,
    string? Category = null,
    string? Note = null,
    long? AssigneeUserId = null,
    DateTime? ExpiryDate = null,
    bool IsRecurring = false);
