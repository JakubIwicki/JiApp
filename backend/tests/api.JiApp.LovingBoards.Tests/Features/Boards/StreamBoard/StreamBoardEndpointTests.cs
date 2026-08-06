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

        var loop = Act(fixture, _ => heartbeat.Task);
        await ReleaseOnDedicatedThread(
            () => subscription.Deliver(buffered),
            () => heartbeat.SetResult(false));
        await loop;

        fixture.BodyText.Should().Contain("event: item.added");
        fixture.BodyText.Should().Contain("data: {\"id\":1}");
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

    private static Task ReleaseOnDedicatedThread(params Action[] releases)
        => Task.Factory.StartNew(
            () => { foreach (var release in releases) release(); },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

    private static Task<bool> NeverCompletingHeartbeat() => new TaskCompletionSource<bool>().Task;

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
        private readonly TaskCompletionSource<bool> _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private volatile BoardEvent? _current;

        public void Deliver(BoardEvent ev)
        {
            _current = ev;
            _release.TrySetResult(true);
        }

        public IAsyncEnumerable<BoardEvent> ReadAllAsync(CancellationToken ct) => new Enumerable(this);

        public void Dispose() { }

        private sealed class Enumerable(GatedBoardSubscription owner) : IAsyncEnumerable<BoardEvent>, IAsyncEnumerator<BoardEvent>
        {
            public BoardEvent Current => owner._current!;

            public IAsyncEnumerator<BoardEvent> GetAsyncEnumerator(CancellationToken cancellationToken) => this;

            public ValueTask<bool> MoveNextAsync() => new(owner._release.Task);

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
