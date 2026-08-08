using System.Net;
using System.Net.Http.Json;
using JiApp.Common.Constants;
using JiApp.YtDownloader.Domain;
using JiApp.YtDownloader.Features.Assistant;
using JiApp.YtDownloader.Features.DownloadStatus;
using JiApp.YtDownloader.Features.GetDownloadLink;
using JiApp.YtDownloader.Features.SearchHistory;
using JiApp.YtDownloader.Features.SearchVideos;
using Moq;

namespace JiApp.YtDownloader.Tests.Integration;

/// <summary>
/// Tier A full-pipeline suite for the YtDownloader module: real HTTP through the
/// host's routing → auth middleware → handler → real SQLite store, with IYoutubeClient
/// doubled (Moq) and the background workers removed. Isolation is by unique userId
/// per test — no per-test DB reset.
/// </summary>
public sealed class YtDownloaderPipelineIntegrationTests : IClassFixture<YtDownloaderPipelineWebApplicationFactory>
{
    private const string BaseUrl = "/api/v1/yt";
    private const string VideoId = "dQw4w9WgXcQ";
    private static int _userIdCounter;

    private readonly YtDownloaderPipelineWebApplicationFactory _factory;

    public YtDownloaderPipelineIntegrationTests(YtDownloaderPipelineWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task RejectsSearch_WhenAnonymous()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync($"{BaseUrl}/search", new SearchVideosRequest("test query", null));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RejectsSearch_WhenTokenLacksYtDownloaderAccess()
    {
        var client = _factory.CreateAuthenticatedClient(NextUserId(), Permissions.UsersManage);

        var response = await client.PostAsJsonAsync($"{BaseUrl}/search", new SearchVideosRequest("test query", null));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PersistsSearchHistory_WhenSearchSucceeds()
    {
        var userId = NextUserId();
        var client = _factory.CreateAuthenticatedClient(userId, Permissions.YtDownloaderAccess);

        var response = await client.PostAsJsonAsync($"{BaseUrl}/search", new SearchVideosRequest("test query", null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.InFreshScope(db => db.YoutubeSearchHistory
            .Any(h => h.UserId == userId && h.SearchText == "test query")).Should().BeTrue();
    }

    [Fact]
    public async Task PersistsSearchHistoryList_AndArchives()
    {
        var userId = NextUserId();
        var client = _factory.CreateAuthenticatedClient(userId, Permissions.YtDownloaderAccess);

        var searchResponse = await client.PostAsJsonAsync($"{BaseUrl}/search", new SearchVideosRequest("another query", null));
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var historyResponse = await client.GetAsync($"{BaseUrl}/search/history?limit=10");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await historyResponse.Content.ReadFromJsonAsync<SearchHistoryResponse>();
        history.Should().NotBeNull();
        history!.Items.Should().ContainSingle(i => i.SearchText == "another query");
        var entryId = history.Items.Single(i => i.SearchText == "another query").Id;

        var archiveResponse = await client.PatchAsJsonAsync($"{BaseUrl}/search/history/{entryId}/archive", new { });
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var archived = _factory.InFreshScope(db => db.YoutubeSearchHistory.Find(entryId));
        archived.Should().NotBeNull();
        archived!.IsArchived.Should().BeTrue();
    }

    [Fact]
    public async Task QueuesDownloadCommand_WhenDownloadRequested()
    {
        var userId = NextUserId();
        var client = _factory.CreateAuthenticatedClient(userId, Permissions.YtDownloaderAccess);

        var response = await client.PostAsJsonAsync($"{BaseUrl}/downloads/mp3", new DownloadRequest(
            VideoId, $"https://youtube.com/watch?v={VideoId}", null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DownloadResponse>();
        body.Should().NotBeNull();
        body!.TempId.Should().NotBeNullOrWhiteSpace();

        var command = _factory.InFreshScope(db => db.DownloadCommands.Find(body.TempId));
        command.Should().NotBeNull();
        command!.Status.Should().Be(DownloadCommandStatus.Queued);
    }

    [Fact]
    public async Task ReportsPending_WhenDownloadStatusQueried()
    {
        var userId = NextUserId();
        var client = _factory.CreateAuthenticatedClient(userId, Permissions.YtDownloaderAccess);

        var createResponse = await client.PostAsJsonAsync($"{BaseUrl}/downloads/mp3", new DownloadRequest(
            VideoId, $"https://youtube.com/watch?v={VideoId}", null, null, null));
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tempId = (await createResponse.Content.ReadFromJsonAsync<DownloadResponse>())!.TempId;

        var response = await client.GetAsync($"{BaseUrl}/downloads/mp3/status/{tempId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DownloadStatusResponse>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("pending");
    }

    [Fact]
    public async Task ReturnsNotFound_WhenPreviewIdFailsRouteConstraint_WithoutExternalCall()
    {
        var client = _factory.CreateAuthenticatedClient(NextUserId(), Permissions.YtDownloaderAccess);

        var response = await client.GetAsync($"{BaseUrl}/preview/short");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _factory.YoutubeClientMock.Verify(c => c.BuildPreviewAudioProcess(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ReturnsServiceUnavailable_WhenAssistantNotConfigured()
    {
        var client = _factory.CreateAuthenticatedClient(NextUserId(), Permissions.YtDownloaderAccess);
        var request = new AssistantChatRequest(
            [new ChatMessageDto(ChatMessageRoles.User, "hi")], "en");

        var response = await client.PostAsJsonAsync($"{BaseUrl}/assistant/chat", request);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    private static long NextUserId() => Interlocked.Increment(ref _userIdCounter);
}
