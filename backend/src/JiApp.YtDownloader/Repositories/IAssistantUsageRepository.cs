namespace JiApp.YtDownloader.Repositories;

public interface IAssistantUsageRepository
{
    Task<bool> TryConsumeAsync(long userId, int limit, CancellationToken ct = default);
}
