namespace JiApp.YtDownloader.Features.Assistant;

/// <summary>
/// Process-wide (single-instance) gate limiting concurrent assistant chat SSE streams to 1.
/// Protects RAM on small instances (t4g.nano / 512 MB) from dual-session pressure.
/// Horizontal scaling would need a distributed lock — this gate does not coordinate across instances.
/// </summary>
public sealed class AssistantStreamGate
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>Try to enter the gate without blocking. Returns true if acquired.</summary>
    public bool TryEnter() => _semaphore.Wait(0);

    /// <summary>Release the gate so another stream can proceed.</summary>
    public void Release() => _semaphore.Release();
}
