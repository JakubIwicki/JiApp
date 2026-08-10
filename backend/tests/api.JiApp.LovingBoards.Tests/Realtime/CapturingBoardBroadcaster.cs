using api.JiApp.LovingBoards.Realtime;

namespace api.JiApp.LovingBoards.Tests.Realtime;

public sealed class CapturingBoardBroadcaster : IBoardBroadcaster
{
    private readonly List<(long BoardId, BoardEvent Ev)> _published = new();
    private readonly List<(long BoardId, long UserId)> _disconnected = new();
    private readonly List<long> _disconnectedAll = new();

    public IReadOnlyList<(long BoardId, BoardEvent Ev)> Published => _published;

    public IBoardSubscription Subscribe(long boardId, long userId) =>
        throw new NotSupportedException("CapturingBoardBroadcaster does not support subscriptions");

    public void Publish(long boardId, BoardEvent ev) =>
        _published.Add((boardId, ev));

    public void Disconnect(long boardId, long userId) =>
        _disconnected.Add((boardId, userId));

    public void DisconnectAll(long boardId) =>
        _disconnectedAll.Add(boardId);

    public void AssertDisconnected(long boardId, long userId) =>
        _disconnected.Should().Contain((boardId, userId),
            $"Expected {boardId}/{userId} to be disconnected, but only these were recorded: {Format(_disconnected)}");

    public void AssertNotDisconnected(long boardId, long userId) =>
        _disconnected.Should().NotContain((boardId, userId),
            $"Expected {boardId}/{userId} NOT to be disconnected, but it was");

    public void AssertDisconnectedAll(long boardId) =>
        _disconnectedAll.Should().Contain(boardId,
            $"Expected all streams on board {boardId} to be disconnected, but only these boards were recorded: {string.Join(", ", _disconnectedAll)}");

    public void AssertNotDisconnectedAll(long boardId) =>
        _disconnectedAll.Should().NotContain(boardId,
            $"Expected streams on board {boardId} NOT to all be disconnected, but they were");

    private static string Format(List<(long BoardId, long UserId)> disconnects) =>
        string.Join(", ", disconnects.Select(d => $"{d.BoardId}/{d.UserId}"));
}
