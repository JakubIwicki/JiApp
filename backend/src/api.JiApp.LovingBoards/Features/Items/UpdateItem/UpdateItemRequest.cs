using api.JiApp.LovingBoards.Common;

namespace api.JiApp.LovingBoards.Features.Items.UpdateItem;

[Serializable]
public sealed record UpdateItemRequest(
    Optional<string> Title = default,
    Optional<string?> Quantity = default,
    Optional<string?> Category = default,
    Optional<string?> Note = default,
    Optional<long?> AssigneeUserId = default,
    Optional<DateTime?> ExpiryDate = default,
    Optional<bool> IsRecurring = default);
