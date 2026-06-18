using JiApp.YtDownloader.Repositories;

namespace JiApp.YtDownloader.Features.Assistant;

public enum AssistantChatPreCheck
{
    Ok,
    QuotaExceeded,
    NotConfigured
}

public sealed class AssistantChatHandler(
    IAssistantUsageRepository usage,
    IAssistantChatClientProvider chatClientProvider)
{
    public async Task<AssistantChatPreCheck> PreCheckAsync(long userId, int dailyLimit, CancellationToken ct)
    {
        if (!chatClientProvider.IsConfigured)
            return AssistantChatPreCheck.NotConfigured;

        return await usage.TryConsumeAsync(userId, dailyLimit, ct)
            ? AssistantChatPreCheck.Ok
            : AssistantChatPreCheck.QuotaExceeded;
    }
}
