using System.ClientModel;
using System.Runtime.CompilerServices;
using JiApp.Common.Resilience;
using Microsoft.Extensions.AI;
using Polly;

namespace JiApp.YtDownloader.Features.Assistant;

/// <summary>
/// Decorates an <see cref="IChatClient"/> with transient-failure retry at stream start.
/// Once the first response update has been produced, a failure is mid-stream and is
/// never retried — retrying would re-run the whole request and duplicate output.
/// </summary>
public sealed class RetryingChatClient(IChatClient inner, IRetryPolicyFactory retryPolicyFactory, int retries = 3) : IChatClient
{
    private readonly ResiliencePipeline _retryPipeline =
        retryPolicyFactory.RetryOnTransientHttp_WithExponentialBackoff(retries: retries, shouldRetry: IsRetryable);

    /// <summary>
    /// Adds the SDK's <see cref="ClientResultException"/> for rate-limit (429) and server
    /// (5xx) statuses, plus the connection-level failure envelope: a transport error surfaces
    /// as a <see cref="ClientResultException"/> with status 0 wrapping the original
    /// <see cref="HttpRequestException"/> — a top-level <c>is HttpRequestException</c> would
    /// miss it because the default transient set does not unwrap InnerException.
    /// </summary>
    private static bool IsRetryable(Exception exception) =>
        exception is ClientResultException { Status: 429 or >= 500 }
        || exception is ClientResultException { Status: 0 } && exception.InnerException is HttpRequestException;

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var capturedMessages = messages.ToList();

        var stream = await _retryPipeline.ExecuteAsync(
            async ct =>
            {
                var enumerator = inner.GetStreamingResponseAsync(capturedMessages, options, ct)
                    .GetAsyncEnumerator(ct);
                try
                {
                    if (await enumerator.MoveNextAsync())
                        return new StartedStream(enumerator, enumerator.Current);
                }
                catch
                {
                    // Dispose must not mask the retryable original exception — a dispose
                    // failure would make Polly decide on the wrong exception.
                    await TryDisposeAsync(enumerator);
                    throw;
                }

                await TryDisposeAsync(enumerator);
                return null;
            },
            cancellationToken);

        if (stream is null)
            yield break;

        await using (stream)
        {
            yield return stream.First;
            while (await stream.Enumerator.MoveNextAsync())
                yield return stream.Enumerator.Current;
        }
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var capturedMessages = messages.ToList();
        return await _retryPipeline.ExecuteAsync(
            async ct => await inner.GetResponseAsync(capturedMessages, options, ct),
            cancellationToken);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        inner.GetService(serviceType, serviceKey);

    public void Dispose() => inner.Dispose();

    private static async ValueTask TryDisposeAsync(IAsyncEnumerator<ChatResponseUpdate> enumerator)
    {
        try { await enumerator.DisposeAsync(); }
        catch { }
    }

    private sealed class StartedStream(IAsyncEnumerator<ChatResponseUpdate> enumerator, ChatResponseUpdate first) : IAsyncDisposable
    {
        public IAsyncEnumerator<ChatResponseUpdate> Enumerator { get; } = enumerator;

        public ChatResponseUpdate First { get; } = first;

        public ValueTask DisposeAsync() => Enumerator.DisposeAsync();
    }
}
