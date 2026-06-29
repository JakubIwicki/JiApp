namespace api.JiApp.LovingBoards.Realtime;

public interface IBoardSubscription : IDisposable
{
    IAsyncEnumerable<BoardEvent> ReadAllAsync(CancellationToken ct);
}
