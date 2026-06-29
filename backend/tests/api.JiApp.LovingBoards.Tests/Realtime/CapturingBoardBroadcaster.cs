using api.JiApp.LovingBoards.Realtime;

namespace api.JiApp.LovingBoards.Tests.Realtime;

public sealed class CapturingBoardBroadcaster : IBoardBroadcaster
{
    private readonly List<(long BoardId, BoardEvent Ev)> _published = new();

    public IReadOnlyList<(long BoardId, BoardEvent Ev)> Published => _published;

    public IBoardSubscription Subscribe(long boardId, long userId) =>
        throw new NotSupportedException("CapturingBoardBroadcaster does not support subscriptions");

    public void Publish(long boardId, BoardEvent ev) =>
        _published.Add((boardId, ev));

    public void Disconnect(long boardId, long userId)
    {
        // capturing only — no-op
    }

    public void DisconnectAll(long boardId)
    {
        // capturing only — no-op
    }
}
