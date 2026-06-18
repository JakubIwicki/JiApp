using JiApp.YtApi;
using JiApp.YtDownloader.Agent;
using JiApp.YtDownloader.Features.Assistant;
using JiApp.YtDownloader.Features.SearchVideos;
using JiApp.YtDownloader.Repositories;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JiApp.YtDownloader.Tests.Features.Assistant;

public class AssistantChatOrchestratorTests
{
    private const long UserId = 42L;
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);

    private sealed class Fixture
    {
        private readonly List<ChatResponseUpdate> _updates = [];
        private Exception? _throwOnStream;

        public FakeChatClient? ChatClient { get; private set; }

        public Fixture WithTextDelta(string text)
        {
            _updates.Add(new ChatResponseUpdate(ChatRole.Assistant, [new TextContent(text)]));
            return this;
        }

        public Fixture WithUpdate(params AIContent[] contents)
        {
            _updates.Add(new ChatResponseUpdate(ChatRole.Assistant, contents));
            return this;
        }

        public Fixture WithFinishReason(ChatFinishReason reason)
        {
            _updates.Add(new ChatResponseUpdate(ChatRole.Assistant, [])
            {
                FinishReason = reason
            });
            return this;
        }

        public Fixture ThrowingOnStream(Exception exception)
        {
            _throwOnStream = exception;
            return this;
        }

        public AssistantChatOrchestrator Build()
        {
            ChatClient = _throwOnStream is not null
                ? FakeChatClient.Throwing(_throwOnStream)
                : new FakeChatClient(_updates);

            var provider = new Mock<IAssistantChatClientProvider>();
            provider.SetupGet(p => p.IsConfigured).Returns(true);
            provider.SetupGet(p => p.Client).Returns(ChatClient);

            var toolService = new YtAgentToolService(
                new Mock<IYoutubeClient>().Object,
                new Mock<ISearchHistoryRepository>().Object,
                new Mock<IDownloadHistoryRepository>().Object,
                new MemoryCache(new MemoryCacheOptions()),
                NullLogger<YtAgentToolService>.Instance);

            return new AssistantChatOrchestrator(
                provider.Object,
                toolService,
                NullLogger<AssistantChatOrchestrator>.Instance);
        }
    }

    private static IReadOnlyList<ChatMessageDto> SingleUserMessage(string text = "find me lofi") =>
        [new ChatMessageDto("user", text)];

    private static async Task<List<AssistantSseEvent>> CollectAsync(
        AssistantChatOrchestrator orchestrator,
        IReadOnlyList<ChatMessageDto> messages,
        string? language,
        CancellationToken ct)
    {
        var events = new List<AssistantSseEvent>();
        await foreach (var ev in orchestrator.StreamAsync(messages, language, UserId, ct))
            events.Add(ev);
        return events;
    }

    [Fact]
    public async Task StreamAsync_text_updates_emit_ordered_text_deltas_then_done_complete()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var orchestrator = new Fixture()
            .WithTextDelta("Hello ")
            .WithTextDelta("world")
            .Build();

        var events = await CollectAsync(orchestrator, SingleUserMessage(), "en", cts.Token);

        events.Should().HaveCount(3);
        events[0].Event.Should().Be(AssistantSseEventNames.TextDelta);
        events[1].Event.Should().Be(AssistantSseEventNames.TextDelta);
        events[2].Event.Should().Be(AssistantSseEventNames.Done);
    }

    [Fact]
    public async Task StreamAsync_prepends_system_prompt_as_first_message()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var fixture = new Fixture().WithTextDelta("hi");
        var orchestrator = fixture.Build();

        await CollectAsync(orchestrator, SingleUserMessage(), "en", cts.Token);

        var captured = fixture.ChatClient!.CapturedMessages!;
        captured.Should().NotBeNull();
        captured[0].Role.Should().Be(ChatRole.System);
        captured[0].Text.Should().Contain("JiApp");
    }

    [Fact]
    public async Task StreamAsync_passes_four_tools_in_chat_options()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var fixture = new Fixture().WithTextDelta("hi");
        var orchestrator = fixture.Build();

        await CollectAsync(orchestrator, SingleUserMessage(), "en", cts.Token);

        var tools = fixture.ChatClient!.CapturedOptions!.Tools!;
        tools.Select(t => t.Name).Should().BeEquivalentTo(
            AssistantToolNames.SearchYoutube,
            AssistantToolNames.ListSearchHistory,
            AssistantToolNames.ListDownloadHistory,
            AssistantToolNames.OfferDownload);
    }

    [Fact]
    public async Task StreamAsync_search_youtube_emits_tool_steps_and_search_results()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        const string callId = "call-1";
        var videos = new SearchVideosResponse(
        [
            new VideoItem("vid1", "Lofi beats", "desc", "img", "url", "channel")
        ]);

        var orchestrator = new Fixture()
            .WithUpdate(new FunctionCallContent(callId, AssistantToolNames.SearchYoutube,
                new Dictionary<string, object?> { ["query"] = "lofi" }!))
            .WithUpdate(new FunctionResultContent(callId, videos.Results))
            .WithTextDelta("Here are some results")
            .Build();

        var events = await CollectAsync(orchestrator, SingleUserMessage(), "en", cts.Token);

        var running = events.Should()
            .ContainSingle(e => e.Event == AssistantSseEventNames.ToolStep
                && ToolStepStatusOf(e) == AssistantToolStepStatus.Running).Subject;
        ToolNameOf(running).Should().Be(AssistantToolNames.SearchYoutube);

        events.Should().ContainSingle(e => e.Event == AssistantSseEventNames.ToolStep
            && ToolStepStatusOf(e) == AssistantToolStepStatus.Done
            && ToolNameOf(e) == AssistantToolNames.SearchYoutube);

        events.Should().ContainSingle(e => e.Event == AssistantSseEventNames.SearchResults);
        events.Last().Event.Should().Be(AssistantSseEventNames.Done);
    }

    [Fact]
    public async Task StreamAsync_offer_download_emits_download_offer_event_without_downloading()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        const string callId = "call-2";
        var offer = new DownloadOffer("vid9", "https://youtu.be/vid9", "A song", "img9");

        var orchestrator = new Fixture()
            .WithUpdate(new FunctionCallContent(callId, AssistantToolNames.OfferDownload,
                new Dictionary<string, object?> { ["videoId"] = "vid9" }!))
            .WithUpdate(new FunctionResultContent(callId, offer))
            .Build();

        var events = await CollectAsync(orchestrator, SingleUserMessage(), "en", cts.Token);

        var offerEvent = events.Should()
            .ContainSingle(e => e.Event == AssistantSseEventNames.DownloadOffer).Subject;
        var dict = ToDictionary(offerEvent.Data);
        dict["videoId"].Should().Be("vid9");
        dict["videoUrl"].Should().Be("https://youtu.be/vid9");
    }

    [Fact]
    public async Task StreamAsync_finish_reason_tool_calls_signals_max_iterations()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var orchestrator = new Fixture()
            .WithTextDelta("working")
            .WithFinishReason(ChatFinishReason.ToolCalls)
            .Build();

        var events = await CollectAsync(orchestrator, SingleUserMessage(), "en", cts.Token);

        var done = events.Last();
        done.Event.Should().Be(AssistantSseEventNames.Done);
        ReasonOf(done).Should().Be(AssistantDoneReasons.MaxIterations);
    }

    [Fact]
    public async Task StreamAsync_exception_emits_done_error_without_leaking_internals()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var orchestrator = new Fixture()
            .ThrowingOnStream(new InvalidOperationException("secret api key sk-12345"))
            .Build();

        var events = await CollectAsync(orchestrator, SingleUserMessage(), "en", cts.Token);

        var done = events.Should().ContainSingle().Subject;
        done.Event.Should().Be(AssistantSseEventNames.Done);
        ReasonOf(done).Should().Be(AssistantDoneReasons.Error);
        System.Text.Json.JsonSerializer.Serialize(done.Data)
            .Should().NotContain("sk-12345");
    }

    private static System.Collections.Generic.IDictionary<string, object?> ToDictionary(object data)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        return System.Text.Json.JsonSerializer
            .Deserialize<Dictionary<string, object?>>(json)!
            .ToDictionary(kv => kv.Key, kv => (object?)kv.Value?.ToString());
    }

    private static string ToolStepStatusOf(AssistantSseEvent e) => ToDictionary(e.Data)["status"] as string ?? "";
    private static string ToolNameOf(AssistantSseEvent e) => ToDictionary(e.Data)["tool"] as string ?? "";
    private static string ReasonOf(AssistantSseEvent e) => ToDictionary(e.Data)["reason"] as string ?? "";
}
