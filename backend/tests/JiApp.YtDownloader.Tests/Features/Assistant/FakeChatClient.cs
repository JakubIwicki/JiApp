using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace JiApp.YtDownloader.Tests.Features.Assistant;

/// <summary>
/// A test double for <see cref="IChatClient"/> that replays a scripted list of
/// <see cref="ChatResponseUpdate"/> values. It captures the messages and options it
/// was called with so tests can assert the system prompt and tools were wired in.
/// </summary>
public sealed class FakeChatClient : IChatClient
{
    private readonly IReadOnlyList<ChatResponseUpdate> _scriptedUpdates;
    private readonly Exception? _throwOnStream;

    public FakeChatClient(IReadOnlyList<ChatResponseUpdate> scriptedUpdates)
    {
        _scriptedUpdates = scriptedUpdates;
    }

    private FakeChatClient(Exception throwOnStream)
    {
        _scriptedUpdates = [];
        _throwOnStream = throwOnStream;
    }

    public static FakeChatClient Throwing(Exception exception) => new(exception);

    public IReadOnlyList<ChatMessage>? CapturedMessages { get; private set; }
    public ChatOptions? CapturedOptions { get; private set; }
    public int StreamCallCount { get; private set; }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        StreamCallCount++;
        CapturedMessages = messages.ToList();
        CapturedOptions = options;

        if (_throwOnStream is not null)
            throw _throwOnStream;

        foreach (var update in _scriptedUpdates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
            await Task.Yield();
        }
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Orchestrator only uses streaming.");

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
