using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using api.JiApp.LovingBoards.Features.Boards.StreamBoard;
using api.JiApp.LovingBoards.Realtime;
using Microsoft.AspNetCore.Http;

namespace api.JiApp.LovingBoards.Tests.Features.Boards.StreamBoard;

public sealed class StreamBoardEndpointTests
{
    [Fact]
    public async Task FlushesBufferedEvent_WhenHeartbeatEndsStream()
    {
        var subscription = new GatedBoardSubscription();
        var fixture = Fixture.Init(subscription);
        var heartbeat = new TaskCompletionSource<bool>();
        var buffered = ItemAddedEvent();
        var syncContext = new ManualSyncContext();
        var previous = SynchronizationContext.Current;

        SynchronizationContext.SetSynchronizationContext(syncContext);
        var loop = Act(fixture, _ => heartbeat.Task);

        try
        {
            // queue the read continuation before the heartbeat wins WhenAny, so the pump can run it
            // first: the delivered event then sits completed-but-undrained on readTask when the loop's
            // heartbeat branch runs, forcing the flush-at-exit branch to write it
            subscription.Deliver(buffered);
            WaitUntilQueued(syncContext);

            // clear the current context so the loop continuation is posted to the manual pump instead
            // of running inline on this thread and breaking before the read had been pumped
            SynchronizationContext.SetSynchronizationContext(null);
            heartbeat.SetResult(false);
            SynchronizationContext.SetSynchronizationContext(syncContext);

            PumpUntilLoopCompletes(syncContext, loop);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        await loop;

        fixture.BodyText.Should().Contain("event: item.added");
        fixture.BodyText.Should().Contain("data: {\"id\":1}");
        subscription.Reads.Should().Be(1);
    }

    [Fact]
    public async Task WritesNothing_WhenHeartbeatEndsStream_WithNoBufferedEvent()
    {
        var fixture = Fixture.Init();

        await Act(fixture, _ => Task.FromResult(false));

        fixture.BodyText.Should().BeEmpty();
    }

    [Fact]
    public async Task FlushesEvents_AsTheyArrive()
    {
        var subscription = new QueuedBoardSubscription();
        subscription.Buffer(ItemAddedEvent(1));
        subscription.Buffer(ItemAddedEvent(2));
        subscription.End();
        var fixture = Fixture.Init(subscription);

        await Act(fixture, _ => NeverCompletingHeartbeat());

        fixture.BodyText.Should().Contain("event: item.added");
        fixture.BodyText.Should().Contain("data: {\"id\":1}");
        fixture.BodyText.Should().Contain("data: {\"id\":2}");
    }

    private static Task Act(Fixture fixture, Func<CancellationToken, Task<bool>> heartbeat)
        => StreamBoardEndpoint.StreamLoopAsync(fixture.Response, fixture.Subscription, heartbeat, CancellationToken.None);

    private static Task<bool> NeverCompletingHeartbeat() => new TaskCompletionSource<bool>().Task;

    private static void WaitUntilQueued(ManualSyncContext syncContext)
    {
        if (!SpinWait.SpinUntil(() => syncContext.HasPending, TimeSpan.FromSeconds(10)))
            throw new TimeoutException("The delivered event was never queued for the stream loop.");
    }

    private static void PumpUntilLoopCompletes(ManualSyncContext syncContext, Task loop)
    {
        var completed = SpinWait.SpinUntil(
            () =>
            {
                if (loop.IsCompleted)
                    return true;

                syncContext.RunPending();
                return false;
            },
            TimeSpan.FromSeconds(10));

        if (!completed)
            throw new TimeoutException("The stream loop did not flush the buffered event before the heartbeat-exit.");
    }

    private static BoardEvent ItemAddedEvent(long id = 1) => new(BoardEventNames.ItemAdded, new { id });

    private sealed class Fixture
    {
        private readonly MemoryStream _body = new();

        private Fixture(IBoardSubscription subscription)
        {
            var context = new DefaultHttpContext();
            context.Response.Body = _body;
            Response = context.Response;
            Subscription = subscription;
        }

        public static Fixture Init() => new(new QueuedBoardSubscription());
        public static Fixture Init(IBoardSubscription subscription) => new(subscription);

        public HttpResponse Response { get; }
        public IBoardSubscription Subscription { get; }
        public string BodyText => Encoding.UTF8.GetString(_body.ToArray());
    }

    private sealed class QueuedBoardSubscription : IBoardSubscription
    {
        private readonly Channel<BoardEvent> _channel = Channel.CreateUnbounded<BoardEvent>();

        public void Buffer(BoardEvent ev) => _channel.Writer.TryWrite(ev);
        public void End() => _channel.Writer.TryComplete();

        public async IAsyncEnumerable<BoardEvent> ReadAllAsync([EnumeratorCancellation] CancellationToken ct)
        {
            await foreach (var ev in _channel.Reader.ReadAllAsync(ct))
                yield return ev;
        }

        public void Dispose() { }
    }

    private sealed class GatedBoardSubscription : IBoardSubscription
    {
        // After the single delivered event is read, further reads must block forever
        // instead of re-yielding the same event — otherwise the loop re-writes it endlessly.
        private static readonly Task<bool> Silence = new TaskCompletionSource<bool>().Task;

        private readonly TaskCompletionSource<bool> _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private volatile BoardEvent? _current;
        private int _reads;

        public void Deliver(BoardEvent ev)
        {
            _current = ev;
            _release.TrySetResult(true);
        }

        public IAsyncEnumerable<BoardEvent> ReadAllAsync(CancellationToken ct) => new Enumerable(this);

        public void Dispose() { }

        public int Reads => _reads;

        private sealed class Enumerable(GatedBoardSubscription owner) : IAsyncEnumerable<BoardEvent>, IAsyncEnumerator<BoardEvent>
        {
            public BoardEvent Current => owner._current!;

            public IAsyncEnumerator<BoardEvent> GetAsyncEnumerator(CancellationToken cancellationToken) => this;

            public ValueTask<bool> MoveNextAsync()
            {
                if (Interlocked.Increment(ref owner._reads) > 1)
                    return new(Silence);

                return new(owner._release.Task);
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    // A SynchronizationContext that parks every posted continuation on a queue the test pumps by
    // hand, so the order in which read and heartbeat continuations resume is fully deterministic.
    private sealed class ManualSyncContext : SynchronizationContext
    {
        private readonly Queue<(SendOrPostCallback Callback, object? State)> _pending = new();
        private readonly object _gate = new();

        public bool HasPending
        {
            get { lock (_gate) return _pending.Count > 0; }
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            lock (_gate)
                _pending.Enqueue((d, state));
        }

        public bool RunPending()
        {
            (SendOrPostCallback Callback, object? State) item;
            lock (_gate)
            {
                if (_pending.Count == 0)
                    return false;
                item = _pending.Dequeue();
            }

            item.Callback(item.State);
            return true;
        }
    }
}
