namespace api.JiApp.LovingBoards.Realtime;

public interface IBoardBroadcaster
{
    IBoardSubscription Subscribe(long boardId, long userId);
    void Publish(long boardId, BoardEvent ev);
    void Disconnect(long boardId, long userId);
    void DisconnectAll(long boardId);
}
