using api.JiApp.LovingBoards.Realtime;

namespace api.JiApp.LovingBoards.Tests.Realtime;

public sealed class NoOpBoardBroadcaster : IBoardBroadcaster
{
    public IBoardSubscription Subscribe(long boardId, long userId) =>
        throw new NotSupportedException("NoOpBoardBroadcaster does not support subscriptions");

    public void Publish(long boardId, BoardEvent ev)
    {
        // no-op
    }
}
