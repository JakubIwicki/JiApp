using JiApp.YtApi;
using JiApp.YtDownloader.Agent;
using JiApp.YtDownloader.Configuration;
using JiApp.YtDownloader.Features.Assistant;
using JiApp.YtDownloader.Features.SearchVideos;
using JiApp.YtDownloader.Repositories;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JiApp.YtDownloader.Tests.Features.Assistant;

public sealed class AssistantChatOrchestratorTests
{
    private const long UserId = 42L;
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);

    private sealed class Fixture
    {
        private readonly List<ChatResponseUpdate> _updates = [];
        private Exception? _throwOnStream;
        private TimeSpan _streamDelay = TimeSpan.Zero;
        private int _requestTimeoutSeconds = 60;

        private AssistantChatOrchestrator? _sut;
        private FakeChatClient? _chatClient;

        public FakeChatClient? ChatClient
        {
            get
            {
                EnsureBuilt();
                return _chatClient;
            }
        }

        public AssistantChatOrchestrator Sut
        {
            get
            {
                EnsureBuilt();
                return _sut!;
            }
        }

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

        public Fixture WithStreamException(Exception exception)
        {
            _throwOnStream = exception;
            return this;
        }

        public Fixture WithStreamDelay(TimeSpan delay)
        {
            _streamDelay = delay;
            return this;
        }

        public Fixture WithRequestTimeoutSeconds(int seconds)
        {
            _requestTimeoutSeconds = seconds;
            return this;
        }

        private void EnsureBuilt()
        {
            if (_sut is not null)
                return;

            _chatClient = _throwOnStream is not null
                ? FakeChatClient.Throwing(_throwOnStream)
                : new FakeChatClient(_updates, _streamDelay);

            var provider = new Mock<IAssistantChatClientProvider>();
            provider.SetupGet(p => p.IsConfigured).Returns(true);
            provider.SetupGet(p => p.Client).Returns(_chatClient);

            var toolService = new YtAgentToolService(
                new Mock<IYoutubeClient>().Object,
                new Mock<ISearchHistoryRepository>().Object,
                new Mock<IDownloadHistoryRepository>().Object,
                new MemoryCache(new MemoryCacheOptions()),
                NullLogger<YtAgentToolService>.Instance);

            var settings = new Settings
            {
                DeepSeek = new Settings.DeepSeekSettings
                {
                    RequestTimeoutSeconds = _requestTimeoutSeconds
                }
            };

            _sut = new AssistantChatOrchestrator(
                provider.Object,
                toolService,
                settings,
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
    public async Task StreamAsync_WithTextUpdates_EmitsOrderedTextDeltasThenDoneComplete()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var orchestrator = new Fixture()
            .WithTextDelta("Hello ")
            .WithTextDelta("world")
            .Sut;

        var events = await CollectAsync(orchestrator, SingleUserMessage(), "en", cts.Token);

        events.Should().HaveCount(3);
        events[0].Event.Should().Be(AssistantSseEventNames.TextDelta);
        events[1].Event.Should().Be(AssistantSseEventNames.TextDelta);
        events[2].Event.Should().Be(AssistantSseEventNames.Done);
    }

    [Fact]
    public async Task StreamAsync_PrependsSystemPrompt_AsFirstMessage()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var fixture = new Fixture().WithTextDelta("hi");

        await CollectAsync(fixture.Sut, SingleUserMessage(), "en", cts.Token);

        var captured = fixture.ChatClient!.CapturedMessages!;
        captured.Should().NotBeNull();
        captured[0].Role.Should().Be(ChatRole.System);
        captured[0].Text.Should().Contain("JiApp");
    }

    [Fact]
    public async Task StreamAsync_PassesFourTools_InChatOptions()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var fixture = new Fixture().WithTextDelta("hi");

        await CollectAsync(fixture.Sut, SingleUserMessage(), "en", cts.Token);

        var tools = fixture.ChatClient!.CapturedOptions!.Tools!;
        tools.Select(t => t.Name).Should().BeEquivalentTo(
            AssistantToolNames.SearchYoutube,
            AssistantToolNames.ListSearchHistory,
            AssistantToolNames.ListDownloadHistory,
            AssistantToolNames.OfferDownload);
    }

    [Fact]
    public async Task StreamAsync_WithSearchYoutubeToolCall_EmitsToolStepsAndSearchResults()
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
            .Sut;

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
    public async Task StreamAsync_WithSearchYoutubeJsonElementResult_EmitsSearchResults()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        const string callId = "call-json-1";
        IReadOnlyList<VideoItem> videos =
        [
            new VideoItem("vid1", "Lofi beats", "desc", "img", "url", "channel")
        ];

        var orchestrator = new Fixture()
            .WithUpdate(new FunctionCallContent(callId, AssistantToolNames.SearchYoutube,
                new Dictionary<string, object?> { ["query"] = "lofi" }!))
            .WithUpdate(new FunctionResultContent(callId, SerializeToJsonElement(videos)))
            .Sut;

        var events = await CollectAsync(orchestrator, SingleUserMessage(), "en", cts.Token);

        var searchEvent = events.Should()
            .ContainSingle(e => e.Event == AssistantSseEventNames.SearchResults).Subject;
        SerializeData(searchEvent.Data).Should().Contain("vid1").And.Contain("Lofi beats");
        events.Last().Event.Should().Be(AssistantSseEventNames.Done);
    }

    [Fact]
    public async Task StreamAsync_WithOfferDownloadJsonElementResult_EmitsDownloadOffer()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        const string callId = "call-json-2";
        var offer = new DownloadOffer("vid9", "https://youtu.be/vid9", "A song", "img9", UserId);

        var orchestrator = new Fixture()
            .WithUpdate(new FunctionCallContent(callId, AssistantToolNames.OfferDownload,
                new Dictionary<string, object?> { ["videoId"] = "vid9" }!))
            .WithUpdate(new FunctionResultContent(callId, SerializeToJsonElement(offer)))
            .Sut;

        var events = await CollectAsync(orchestrator, SingleUserMessage(), "en", cts.Token);

        var offerEvent = events.Should()
            .ContainSingle(e => e.Event == AssistantSseEventNames.DownloadOffer).Subject;
        var dict = ToDictionary(offerEvent.Data);
        dict["videoId"].Should().Be("vid9");
        dict["videoUrl"].Should().Be("https://youtu.be/vid9");
    }

    [Fact]
    public async Task StreamAsync_WithOfferDownloadToolCall_EmitsDownloadOfferEventWithoutDownloading()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        const string callId = "call-2";
        var offer = new DownloadOffer("vid9", "https://youtu.be/vid9", "A song", "img9", UserId);

        var orchestrator = new Fixture()
            .WithUpdate(new FunctionCallContent(callId, AssistantToolNames.OfferDownload,
                new Dictionary<string, object?> { ["videoId"] = "vid9" }!))
            .WithUpdate(new FunctionResultContent(callId, offer))
            .Sut;

        var events = await CollectAsync(orchestrator, SingleUserMessage(), "en", cts.Token);

        var offerEvent = events.Should()
            .ContainSingle(e => e.Event == AssistantSseEventNames.DownloadOffer).Subject;
        var dict = ToDictionary(offerEvent.Data);
        dict["videoId"].Should().Be("vid9");
        dict["videoUrl"].Should().Be("https://youtu.be/vid9");
    }

    [Fact]
    public async Task StreamAsync_WithFinishReasonToolCallsAfterToolInvoked_SignalsMaxIterations()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        const string callId = "call-max";
        var orchestrator = new Fixture()
            .WithUpdate(new FunctionCallContent(callId, AssistantToolNames.SearchYoutube,
                new Dictionary<string, object?> { ["query"] = "lofi" }!))
            .WithFinishReason(ChatFinishReason.ToolCalls)
            .Sut;

        var events = await CollectAsync(orchestrator, SingleUserMessage(), "en", cts.Token);

        var done = events.Last();
        done.Event.Should().Be(AssistantSseEventNames.Done);
        ReasonOf(done).Should().Be(AssistantDoneReasons.MaxIterations);
    }

    [Fact]
    public async Task StreamAsync_WithFinishReasonToolCallsWithoutToolInvoked_SignalsComplete()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var orchestrator = new Fixture()
            .WithTextDelta("working")
            .WithFinishReason(ChatFinishReason.ToolCalls)
            .Sut;

        var events = await CollectAsync(orchestrator, SingleUserMessage(), "en", cts.Token);

        var done = events.Last();
        done.Event.Should().Be(AssistantSseEventNames.Done);
        ReasonOf(done).Should().Be(AssistantDoneReasons.Complete);
    }

    [Fact]
    public async Task StreamAsync_WithRequestTimeout_EmitsDoneErrorAndDoesNotThrow()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var orchestrator = new Fixture()
            .WithRequestTimeoutSeconds(1)
            .WithStreamDelay(TimeSpan.FromSeconds(30))
            .WithTextDelta("never reached")
            .Sut;

        var events = await CollectAsync(orchestrator, SingleUserMessage(), "en", cts.Token);

        var done = events.Should().ContainSingle().Subject;
        done.Event.Should().Be(AssistantSseEventNames.Done);
        ReasonOf(done).Should().Be(AssistantDoneReasons.Error);
    }

    [Fact]
    public async Task StreamAsync_WithCallerCancellation_PropagatesAsCancellationNotErrorFrame()
    {
        using var cts = new CancellationTokenSource();
        var orchestrator = new Fixture()
            .WithStreamDelay(TimeSpan.FromSeconds(30))
            .WithTextDelta("never reached")
            .Sut;

        cts.CancelAfter(TimeSpan.FromMilliseconds(200));

        var act = async () => await CollectAsync(orchestrator, SingleUserMessage(), "en", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task StreamAsync_WithException_EmitsDoneErrorWithoutLeakingInternals()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var orchestrator = new Fixture()
            .WithStreamException(new InvalidOperationException("secret api key sk-12345"))
            .Sut;

        var events = await CollectAsync(orchestrator, SingleUserMessage(), "en", cts.Token);

        var done = events.Should().ContainSingle().Subject;
        done.Event.Should().Be(AssistantSseEventNames.Done);
        ReasonOf(done).Should().Be(AssistantDoneReasons.Error);
        System.Text.Json.JsonSerializer.Serialize(done.Data)
            .Should().NotContain("sk-12345");
    }

    private static readonly System.Text.Json.JsonSerializerOptions WebOptions =
        new(System.Text.Json.JsonSerializerDefaults.Web);

    private static System.Text.Json.JsonElement SerializeToJsonElement<T>(T value) =>
        System.Text.Json.JsonSerializer.SerializeToElement(value, WebOptions);

    private static string SerializeData(object data) =>
        System.Text.Json.JsonSerializer.Serialize(data);

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
