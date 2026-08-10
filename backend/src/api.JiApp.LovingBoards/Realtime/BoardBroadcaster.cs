using System.Collections.Concurrent;
using System.Threading.Channels;

namespace api.JiApp.LovingBoards.Realtime;

/// <summary>
/// In-memory per-board subscriber registry for realtime board events.
/// Single-instance: subscriptions live only in this process, so a second replica
/// would fragment boards across instances. Enforced at startup by
/// <see cref="JiApp.Common.Services.SingleInstanceGuard"/>.
/// </summary>
public sealed class BoardBroadcaster : IBoardBroadcaster
{
    private const int ChannelCapacity = 100;

    private readonly ConcurrentDictionary<long, ConcurrentDictionary<Guid, Subscriber>> _boards = new();

    // Serializes add vs. remove-when-empty on _boards so a Subscribe racing the last
    // Unsubscribe/Disconnect cannot write into an inner map that was already detached.
    private readonly object _sync = new();

    public IBoardSubscription Subscribe(long boardId, long userId)
    {
        var subscriberId = Guid.NewGuid();
        var channel = Channel.CreateBounded<BoardEvent>(new BoundedChannelOptions(ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        var subscriber = new Subscriber(userId, channel);
        lock (_sync)
        {
            var boardSubscribers = _boards.GetOrAdd(boardId, _ => new ConcurrentDictionary<Guid, Subscriber>());
            boardSubscribers[subscriberId] = subscriber;
        }

        BroadcastPresence(boardId);

        return new Subscription(subscriberId, boardId, channel.Reader, this);
    }

    public void Publish(long boardId, BoardEvent ev)
    {
        if (!_boards.TryGetValue(boardId, out var boardSubscribers))
            return;

        foreach (var (_, subscriber) in boardSubscribers)
            subscriber.Channel.Writer.TryWrite(ev);
    }

    public void Disconnect(long boardId, long userId)
    {
        lock (_sync)
        {
            if (!_boards.TryGetValue(boardId, out var boardSubscribers))
                return;

            foreach (var (subscriberId, subscriber) in boardSubscribers)
            {
                if (subscriber.UserId == userId)
                {
                    subscriber.Channel.Writer.TryComplete();
                    boardSubscribers.TryRemove(subscriberId, out _);
                }
            }

            if (boardSubscribers.IsEmpty)
                _boards.TryRemove(boardId, out _);
        }

        BroadcastPresence(boardId);
    }

    public void DisconnectAll(long boardId)
    {
        lock (_sync)
        {
            if (!_boards.TryRemove(boardId, out var boardSubscribers))
                return;

            foreach (var (_, subscriber) in boardSubscribers)
                subscriber.Channel.Writer.TryComplete();
        }
    }

    private void BroadcastPresence(long boardId)
    {
        if (!_boards.TryGetValue(boardId, out var boardSubscribers))
        {
            Publish(boardId, new BoardEvent(BoardEventNames.Presence, new { userIds = Array.Empty<long>() }));
            return;
        }

        var userIds = boardSubscribers.Values
            .Select(s => s.UserId)
            .Distinct()
            .ToArray();

        Publish(boardId, new BoardEvent(BoardEventNames.Presence, new { userIds }));
    }

    private void Unsubscribe(Guid subscriberId, long boardId)
    {
        lock (_sync)
        {
            if (!_boards.TryGetValue(boardId, out var boardSubscribers))
                return;

            if (boardSubscribers.TryRemove(subscriberId, out var removed))
                removed.Channel.Writer.TryComplete();

            if (boardSubscribers.IsEmpty)
                _boards.TryRemove(boardId, out _);
        }

        BroadcastPresence(boardId);
    }

    private sealed record Subscriber(long UserId, Channel<BoardEvent> Channel);

    private sealed class Subscription : IBoardSubscription
    {
        private readonly Guid _subscriberId;
        private readonly long _boardId;
        private readonly ChannelReader<BoardEvent> _reader;
        private readonly BoardBroadcaster _broadcaster;
        private bool _disposed;

        public Subscription(Guid subscriberId, long boardId, ChannelReader<BoardEvent> reader, BoardBroadcaster broadcaster)
        {
            _subscriberId = subscriberId;
            _boardId = boardId;
            _reader = reader;
            _broadcaster = broadcaster;
        }

        public IAsyncEnumerable<BoardEvent> ReadAllAsync(CancellationToken ct) =>
            _reader.ReadAllAsync(ct);

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _broadcaster.Unsubscribe(_subscriberId, _boardId);
        }
    }
}
