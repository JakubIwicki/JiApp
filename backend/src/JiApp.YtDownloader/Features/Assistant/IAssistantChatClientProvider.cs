using Microsoft.Extensions.AI;

namespace JiApp.YtDownloader.Features.Assistant;

public interface IAssistantChatClientProvider
{
    bool IsConfigured { get; }

    IChatClient Client { get; }
}
