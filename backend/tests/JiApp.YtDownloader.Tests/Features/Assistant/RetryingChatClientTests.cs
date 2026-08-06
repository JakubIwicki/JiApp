using System.ClientModel;
using System.ClientModel.Primitives;
using JiApp.Common.Resilience;
using JiApp.YtDownloader.Features.Assistant;
using Microsoft.Extensions.AI;

namespace JiApp.YtDownloader.Tests.Features.Assistant;

public sealed class RetryingChatClientTests
{
    private const int Retries = 1;

    private static readonly ChatMessage[] Messages = [new ChatMessage(ChatRole.User, "hi")];

    private static readonly ChatResponseUpdate[] Updates =
        [new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("hello")])];

    [Fact]
    public async Task GetStreamingResponseAsync_WhenTransientFailureAtStart_RetriesThenSucceeds()
    {
        var inner = new ScriptedChatClient()
            .WithStreams(
                ThrowingStream(UpstreamError(500)),
                YieldingStream(Updates));
        var sut = CreateSut(inner);

        var updates = await CollectAsync(sut);

        updates.Should().Equal(Updates);
        inner.StreamCallCount.Should().Be(2);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_WhenConnectionFailureAtStart_RetriesThenSucceeds()
    {
        // The SDK wraps a network-level failure in a ClientResultException with Status 0
        // and the original HttpRequestException as InnerException.
        var inner = new ScriptedChatClient()
            .WithStreams(
                ThrowingStream(ConnectionFailure()),
                YieldingStream(Updates));
        var sut = CreateSut(inner);

        var updates = await CollectAsync(sut);

        updates.Should().Equal(Updates);
        inner.StreamCallCount.Should().Be(2);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_WhenPersistentTransientFailure_ThrowsAfterRetries()
    {
        var inner = new ScriptedChatClient().WithStreams(ThrowingStream(UpstreamError(503)));
        var sut = CreateSut(inner);

        var act = async () => await CollectAsync(sut);

        await act.Should().ThrowAsync<ClientResultException>();
        inner.StreamCallCount.Should().Be(Retries + 1);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_WhenNonRetryableError_ThrowsImmediately()
    {
        var inner = FakeChatClient.Throwing(new InvalidOperationException("not retryable"));
        var sut = CreateSut(inner);

        var act = async () => await CollectAsync(sut);

        await act.Should().ThrowAsync<InvalidOperationException>();
        inner.StreamCallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_WhenFailsAfterFirstUpdate_DoesNotRetryMidStream()
    {
        var inner = new ScriptedChatClient()
            .WithStreams(YieldingThenThrowingStream(UpstreamError(500), Updates));
        var sut = CreateSut(inner);
        var received = new List<ChatResponseUpdate>();

        var act = async () =>
        {
            await foreach (var update in sut.GetStreamingResponseAsync(Messages))
                received.Add(update);
        };

        await act.Should().ThrowAsync<ClientResultException>();
        received.Should().Equal(Updates);
        inner.StreamCallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_WhenCallerCancelled_IsNotRetried()
    {
        using var cts = new CancellationTokenSource();
        var inner = new ScriptedChatClient().WithStreams(CancellingStream(cts));
        var sut = CreateSut(inner);

        var act = async () => await CollectAsync(sut, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        inner.StreamCallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetResponseAsync_WhenTransientFailure_RetriesThenSucceeds()
    {
        var inner = new ScriptedChatClient().WithResponses(
            _ => throw UpstreamError(500),
            _ => Task.FromResult(new ChatResponse()));
        var sut = CreateSut(inner);

        var response = await sut.GetResponseAsync(Messages);

        response.Should().NotBeNull();
        inner.ResponseCallCount.Should().Be(2);
    }

    private static RetryingChatClient CreateSut(IChatClient inner) =>
        new(inner, new RetryPolicyFactory(TimeProvider.System), retries: Retries);

    private static async Task<List<ChatResponseUpdate>> CollectAsync(
        RetryingChatClient sut, CancellationToken ct = default)
    {
        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in sut.GetStreamingResponseAsync(Messages, cancellationToken: ct))
            updates.Add(update);
        return updates;
    }

    private static ClientResultException UpstreamError(int status) =>
        new("upstream error", new FakePipelineResponse(status), null);

    private static ClientResultException ConnectionFailure() =>
        new("connection failed", new FakePipelineResponse(0), new HttpRequestException("network down"));

    private static IAsyncEnumerable<ChatResponseUpdate> ThrowingStream(Exception exception) =>
        ThrowingStreamCore(exception);

    private static async IAsyncEnumerable<ChatResponseUpdate> ThrowingStreamCore(Exception exception)
    {
        if (exception is not null)
            throw exception;
        yield break;
    }

    private static IAsyncEnumerable<ChatResponseUpdate> YieldingStream(params ChatResponseUpdate[] updates) =>
        YieldingStreamCore(updates);

    private static async IAsyncEnumerable<ChatResponseUpdate> YieldingStreamCore(ChatResponseUpdate[] updates)
    {
        foreach (var update in updates)
        {
            yield return update;
            await Task.CompletedTask;
        }
    }

    private static IAsyncEnumerable<ChatResponseUpdate> YieldingThenThrowingStream(
        Exception exception, params ChatResponseUpdate[] updates) =>
        YieldingThenThrowingStreamCore(exception, updates);

    private static async IAsyncEnumerable<ChatResponseUpdate> YieldingThenThrowingStreamCore(
        Exception exception, ChatResponseUpdate[] updates)
    {
        foreach (var update in updates)
            yield return update;
        throw exception;
    }

    private static IAsyncEnumerable<ChatResponseUpdate> CancellingStream(CancellationTokenSource cts) =>
        CancellingStreamCore(cts);

    private static async IAsyncEnumerable<ChatResponseUpdate> CancellingStreamCore(CancellationTokenSource cts)
    {
        if (cts is not null)
        {
            cts.Cancel();
            throw new OperationCanceledException(cts.Token);
        }
        yield break;
    }

    private sealed class ScriptedChatClient : IChatClient
    {
        private readonly List<IAsyncEnumerable<ChatResponseUpdate>> _streams = [];
        private readonly List<Func<CancellationToken, Task<ChatResponse>>> _responses = [];

        public int StreamCallCount { get; private set; }

        public int ResponseCallCount { get; private set; }

        public ScriptedChatClient WithStreams(params IAsyncEnumerable<ChatResponseUpdate>[] streams)
        {
            _streams.AddRange(streams);
            return this;
        }

        public ScriptedChatClient WithResponses(params Func<CancellationToken, Task<ChatResponse>>[] responses)
        {
            _responses.AddRange(responses);
            return this;
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            StreamCallCount++;
            var index = Math.Min(StreamCallCount - 1, _streams.Count - 1);
            return _streams[Math.Max(index, 0)];
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            ResponseCallCount++;
            var index = Math.Min(ResponseCallCount - 1, _responses.Count - 1);
            return _responses[Math.Max(index, 0)](cancellationToken);
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    private sealed class FakePipelineResponse(int status) : PipelineResponse
    {
        private readonly FakeHeaders _headers = new();

        public override int Status { get; } = status;

        public override string ReasonPhrase => string.Empty;

        public override Stream? ContentStream { get; set; }

        public override BinaryData Content => BinaryData.FromBytes([]);

        protected override PipelineResponseHeaders HeadersCore => _headers;

        public override BinaryData BufferContent(CancellationToken cancellationToken = default) => Content;

        public override ValueTask<BinaryData> BufferContentAsync(CancellationToken cancellationToken = default) =>
            new(Content);

        public override void Dispose()
        {
        }
    }

    private sealed class FakeHeaders : PipelineResponseHeaders
    {
        public override IEnumerator<KeyValuePair<string, string>> GetEnumerator() =>
            Enumerable.Empty<KeyValuePair<string, string>>().GetEnumerator();

        public override bool TryGetValue(string name, out string? value)
        {
            value = null;
            return false;
        }

        public override bool TryGetValues(string name, out IEnumerable<string>? values)
        {
            values = null;
            return false;
        }
    }
}
