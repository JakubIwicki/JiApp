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
        if (!await usage.TryConsumeAsync(userId, dailyLimit, ct))
            return AssistantChatPreCheck.QuotaExceeded;

        return chatClientProvider.IsConfigured
            ? AssistantChatPreCheck.Ok
            : AssistantChatPreCheck.NotConfigured;
    }
}
