using System.Runtime.CompilerServices;
using System.Text.Json;
using JiApp.YtDownloader.Agent;
using JiApp.YtDownloader.Configuration;
using JiApp.YtDownloader.Features.DownloadHistory;
using JiApp.YtDownloader.Features.SearchHistory;
using JiApp.YtDownloader.Features.SearchVideos;
using JiApp.YtDownloader.Logging;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JiApp.YtDownloader.Features.Assistant;

public sealed class AssistantChatOrchestrator(
    IAssistantChatClientProvider chatClientProvider,
    YtAgentToolService toolService,
    Settings settings,
    ILogger<AssistantChatOrchestrator> logger)
{
    private const int DefaultRequestTimeoutSeconds = 60;

    private static readonly JsonSerializerOptions ToolResultJsonOptions =
        new(JsonSerializerDefaults.Web);

    public async IAsyncEnumerable<AssistantSseEvent> StreamAsync(
        IReadOnlyList<ChatMessageDto> messages,
        string? language,
        long userId,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var chatClient = chatClientProvider.Client;
        var chatMessages = BuildChatMessages(messages, language);

        var timeoutSeconds = settings.DeepSeek?.RequestTimeoutSeconds is int configured and > 0
            ? configured
            : DefaultRequestTimeoutSeconds;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        var turnToken = cts.Token;

        var options = new ChatOptions { Tools = BuildTools(userId, turnToken) };

        var callIdToToolName = new Dictionary<string, string>();
        ChatFinishReason? lastFinishReason = null;
        var anyToolInvoked = false;
        var faulted = false;

        var stream = chatClient.GetStreamingResponseAsync(chatMessages, options, turnToken)
            .GetAsyncEnumerator(turnToken);

        try
        {
            while (true)
            {
                AssistantSseEvent[]? mapped;
                try
                {
                    if (!await stream.MoveNextAsync())
                        break;

                    var update = stream.Current;
                    if (update.FinishReason is { } finishReason)
                        lastFinishReason = finishReason;

                    mapped = MapUpdate(update, callIdToToolName, ref anyToolInvoked);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (OperationCanceledException)
                {
                    logger.AssistantChatTurnTimedOut(userId, timeoutSeconds);
                    faulted = true;
                    break;
                }
                catch (Exception ex)
                {
                    logger.AssistantChatStreamFailed(userId, ex.GetType().Name, Redact(ex.Message));
                    faulted = true;
                    break;
                }

                foreach (var ev in mapped)
                    yield return ev;
            }
        }
        finally
        {
            await stream.DisposeAsync();
        }

        yield return new AssistantSseEvent(
            AssistantSseEventNames.Done,
            new { reason = ResolveDoneReason(faulted, lastFinishReason, anyToolInvoked) });
    }

    private static string ResolveDoneReason(
        bool faulted, ChatFinishReason? lastFinishReason, bool anyToolInvoked)
    {
        if (faulted)
            return AssistantDoneReasons.Error;

        return lastFinishReason == ChatFinishReason.ToolCalls && anyToolInvoked
            ? AssistantDoneReasons.MaxIterations
            : AssistantDoneReasons.Complete;
    }

    private const int MaxLoggedMessageLength = 200;

    private static string Redact(string message)
    {
        var truncated = message.Length > MaxLoggedMessageLength
            ? message[..MaxLoggedMessageLength]
            : message;
        return truncated;
    }

    private static List<ChatMessage> BuildChatMessages(
        IReadOnlyList<ChatMessageDto> messages, string? language)
    {
        var chatMessages = new List<ChatMessage>(messages.Count + 1)
        {
            new(ChatRole.System, SystemPrompt.Build(language))
        };

        foreach (var message in messages)
        {
            var role = message.Role == ChatMessageRoles.Assistant
                ? ChatRole.Assistant
                : ChatRole.User;
            chatMessages.Add(new ChatMessage(role, message.Content));
        }

        return chatMessages;
    }

    private AssistantSseEvent[] MapUpdate(
        ChatResponseUpdate update,
        Dictionary<string, string> callIdToToolName,
        ref bool anyToolInvoked)
    {
        var events = new List<AssistantSseEvent>();

        foreach (var content in update.Contents)
        {
            switch (content)
            {
                case TextContent { Text.Length: > 0 } text:
                    events.Add(new AssistantSseEvent(
                        AssistantSseEventNames.TextDelta, new { text = text.Text }));
                    break;

                case FunctionCallContent call:
                    anyToolInvoked = true;
                    callIdToToolName[call.CallId] = call.Name;
                    events.Add(new AssistantSseEvent(
                        AssistantSseEventNames.ToolStep,
                        new { tool = call.Name, status = AssistantToolStepStatus.Running }));
                    break;

                case FunctionResultContent result:
                    var toolName = callIdToToolName.GetValueOrDefault(result.CallId, "unknown");
                    events.Add(new AssistantSseEvent(
                        AssistantSseEventNames.ToolStep,
                        new { tool = toolName, status = AssistantToolStepStatus.Done }));

                    var domainEvent = MapToolResult(toolName, result.Result);
                    if (domainEvent is not null)
                        events.Add(domainEvent);
                    break;
            }
        }

        return [.. events];
    }

    private AssistantSseEvent? MapToolResult(string toolName, object? result) => toolName switch
    {
        AssistantToolNames.SearchYoutube =>
            Deserialize<IReadOnlyList<VideoItem>, List<VideoItem>>(result, toolName) is { } videos
                ? new AssistantSseEvent(AssistantSseEventNames.SearchResults, new { results = videos })
                : null,
        AssistantToolNames.OfferDownload =>
            Deserialize<DownloadOffer, DownloadOffer>(result, toolName) is { } offer
                ? new AssistantSseEvent(AssistantSseEventNames.DownloadOffer, new
                {
                    videoId = offer.VideoId,
                    videoUrl = offer.VideoUrl,
                    title = offer.Title,
                    imageUrl = offer.ImageUrl
                })
                : null,
        _ => null
    };

    private TTyped? Deserialize<TTyped, TJson>(object? result, string toolName)
        where TTyped : class
        where TJson : class, TTyped
    {
        switch (result)
        {
            case TTyped typed:
                return typed;
            case JsonElement element:
                try
                {
                    return element.Deserialize<TJson>(ToolResultJsonOptions);
                }
                catch (JsonException ex)
                {
                    logger.AssistantToolResultDeserializeFailed(ex, toolName);
                    return null;
                }
            default:
                return null;
        }
    }

    private IList<AITool> BuildTools(long userId, CancellationToken ct)
    {
        async Task<object> SearchYoutubeAsync(string query, int? maxResults)
        {
            var result = await toolService.SearchAsync(userId, query, maxResults, ct);
            return result is { IsSuccess: true, Value: not null }
                ? result.Value.Results
                : result.Error ?? "Search failed.";
        }

        async Task<object> ListSearchHistoryAsync(int? limit)
        {
            var result = await toolService.ListSearchHistoryAsync(userId, limit, ct);
            return result is { IsSuccess: true, Value: not null }
                ? result.Value.Items
                : result.Error ?? "Failed to load search history.";
        }

        async Task<object> ListDownloadHistoryAsync(int? limit)
        {
            var result = await toolService.ListDownloadHistoryAsync(userId, limit, ct);
            return result is { IsSuccess: true, Value: not null }
                ? result.Value.Items
                : result.Error ?? "Failed to load download history.";
        }

        DownloadOffer OfferDownload(string videoId, string videoUrl, string? title, string? imageUrl) =>
            toolService.BuildDownloadOffer(videoId, videoUrl, title, imageUrl);

        return
        [
            AIFunctionFactory.Create(
                SearchYoutubeAsync,
                AssistantToolNames.SearchYoutube,
                "Search YouTube for music videos matching a query (or a YouTube URL). "
                + "Returns a list of videos with id, title, channel, thumbnail and url."),
            AIFunctionFactory.Create(
                ListSearchHistoryAsync,
                AssistantToolNames.ListSearchHistory,
                "List the user's most recent past search queries."),
            AIFunctionFactory.Create(
                ListDownloadHistoryAsync,
                AssistantToolNames.ListDownloadHistory,
                "List the user's most recently downloaded videos."),
            AIFunctionFactory.Create(
                (string videoId, string videoUrl, string? title, string? imageUrl) =>
                    OfferDownload(videoId, videoUrl, title, imageUrl),
                AssistantToolNames.OfferDownload,
                "Propose a download to the user by showing a confirmation card. This does NOT "
                + "download anything; the user must tap the card to confirm and start the download. "
                + "Never claim a download happened.")
        ];
    }
}
