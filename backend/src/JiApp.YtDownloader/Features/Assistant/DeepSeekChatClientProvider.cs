using System.ClientModel;
using JiApp.YtDownloader.Configuration;
using Microsoft.Extensions.AI;
using OpenAI;

namespace JiApp.YtDownloader.Features.Assistant;

public sealed class DeepSeekChatClientProvider : IAssistantChatClientProvider
{
    private readonly IChatClient? _client;

    public DeepSeekChatClientProvider(Settings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var deepSeek = settings.DeepSeek;
        if (deepSeek is null || string.IsNullOrWhiteSpace(deepSeek.ApiKey))
        {
            _client = null;
            return;
        }

        var baseUrl = string.IsNullOrWhiteSpace(deepSeek.BaseUrl)
            ? "https://api.deepseek.com"
            : deepSeek.BaseUrl;
        var model = string.IsNullOrWhiteSpace(deepSeek.Model)
            ? "deepseek-chat"
            : deepSeek.Model;

        var openAiClient = new OpenAIClient(
            new ApiKeyCredential(deepSeek.ApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(baseUrl) });

        _client = openAiClient
            .GetChatClient(model)
            .AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation(configure: c => c.MaximumIterationsPerRequest = deepSeek.MaxIterations)
            .Build();
    }

    public bool IsConfigured => _client is not null;

    public IChatClient Client => _client
        ?? throw new InvalidOperationException(
            "DeepSeek assistant is not configured. Check IsConfigured before accessing Client.");
}
