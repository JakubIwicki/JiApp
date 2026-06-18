using System.Runtime.CompilerServices;
using JiApp.YtDownloader.Agent;
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
    ILogger<AssistantChatOrchestrator> logger)
{
    public async IAsyncEnumerable<AssistantSseEvent> StreamAsync(
        IReadOnlyList<ChatMessageDto> messages,
        string? language,
        long userId,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var chatClient = chatClientProvider.Client;
        var chatMessages = BuildChatMessages(messages, language);
        var options = new ChatOptions { Tools = BuildTools(userId, ct) };

        var callIdToToolName = new Dictionary<string, string>();
        ChatFinishReason? lastFinishReason = null;
        var faulted = false;

        var stream = chatClient.GetStreamingResponseAsync(chatMessages, options, ct)
            .GetAsyncEnumerator(ct);

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

                    mapped = MapUpdate(update, callIdToToolName);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.AssistantChatStreamFailed(ex, userId);
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
            new { reason = ResolveDoneReason(faulted, lastFinishReason) });
    }

    private static string ResolveDoneReason(bool faulted, ChatFinishReason? lastFinishReason)
    {
        if (faulted)
            return AssistantDoneReasons.Error;

        return lastFinishReason == ChatFinishReason.ToolCalls
            ? AssistantDoneReasons.MaxIterations
            : AssistantDoneReasons.Complete;
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

    private static AssistantSseEvent[] MapUpdate(
        ChatResponseUpdate update,
        Dictionary<string, string> callIdToToolName)
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

    private static AssistantSseEvent? MapToolResult(string toolName, object? result) => result switch
    {
        IReadOnlyList<VideoItem> videos when toolName == AssistantToolNames.SearchYoutube =>
            new AssistantSseEvent(AssistantSseEventNames.SearchResults, new { results = videos }),
        DownloadOffer offer when toolName == AssistantToolNames.OfferDownload =>
            new AssistantSseEvent(AssistantSseEventNames.DownloadOffer, new
            {
                videoId = offer.VideoId,
                videoUrl = offer.VideoUrl,
                title = offer.Title,
                imageUrl = offer.ImageUrl
            }),
        _ => null
    };

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
