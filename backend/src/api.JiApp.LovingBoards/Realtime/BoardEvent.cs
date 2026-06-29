namespace api.JiApp.LovingBoards.Realtime;

public sealed record BoardEvent(string Event, object Data);

public static class BoardEventNames
{
    public const string ItemAdded = "item.added";
    public const string ItemUpdated = "item.updated";
    public const string ItemStatus = "item.status";
    public const string ItemRemoved = "item.removed";
    public const string ItemsCleared = "items.cleared";
    public const string BoardUpdated = "board.updated";
    public const string MemberChanged = "member.changed";
    public const string RecurringReset = "recurring.reset";
    public const string BoardDeleted = "board.deleted";
    public const string Presence = "presence";
}
