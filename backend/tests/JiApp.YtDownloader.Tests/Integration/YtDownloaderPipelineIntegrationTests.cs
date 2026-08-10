using System.Net;
using System.Net.Http.Json;
using JiApp.Common.Constants;
using JiApp.Common.Models;
using JiApp.YtDownloader.Domain;
using JiApp.YtDownloader.Features.Assistant;
using JiApp.YtDownloader.Features.DownloadHistory;
using JiApp.YtDownloader.Features.DownloadStatus;
using JiApp.YtDownloader.Features.GetDownloadLink;
using JiApp.YtDownloader.Features.GetHistory;
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
        var userId = NextUserId();
        var client = _factory.CreateAuthenticatedClient(userId, Permissions.UsersManage);

        var response = await client.PostAsJsonAsync($"{BaseUrl}/search", new SearchVideosRequest("test query", null));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _factory.InFreshScope(db => db.YoutubeSearchHistory.Count(h => h.UserId == userId)).Should().Be(0);
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

    [Fact]
    public async Task ServesFile_ForReadyJobOwnedByCaller()
    {
        var userId = NextUserId();
        var client = _factory.CreateAuthenticatedClient(userId, Permissions.YtDownloaderAccess);
        var filePath = Path.GetTempFileName();
        var bytes = new byte[] { 0x49, 0x44, 0x33, 0x01 };
        File.WriteAllBytes(filePath, bytes);
        try
        {
            var tempId = SeedReadyJob(userId, filePath);

            var response = await client.GetAsync($"{BaseUrl}/downloads/mp3/file/{tempId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.Should().Be("audio/mpeg");
            (await response.Content.ReadAsByteArrayAsync()).Should().BeEquivalentTo(bytes);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ReturnsNotFound_ForUnknownTempId_OnFileDownload()
    {
        var userId = NextUserId();
        var client = _factory.CreateAuthenticatedClient(userId, Permissions.YtDownloaderAccess);

        var response = await client.GetAsync($"{BaseUrl}/downloads/mp3/file/does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnsNotFound_ForAnotherUsersReadyJob_OnFileDownload()
    {
        var userId = NextUserId();
        var otherUserId = NextUserId();
        var client = _factory.CreateAuthenticatedClient(userId, Permissions.YtDownloaderAccess);
        var filePath = Path.GetTempFileName();
        File.WriteAllBytes(filePath, [0x49, 0x44, 0x33]);
        try
        {
            var tempId = SeedReadyJob(otherUserId, filePath);

            var response = await client.GetAsync($"{BaseUrl}/downloads/mp3/file/{tempId}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task RejectsFileDownload_WhenAnonymous()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{BaseUrl}/downloads/mp3/file/some-id");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReturnsOwnersDownloadHistory_HonouringLimit()
    {
        var userId = NextUserId();
        var otherUserId = NextUserId();
        var client = _factory.CreateAuthenticatedClient(userId, Permissions.YtDownloaderAccess);
        SeedDownloadHistory(userId, "vid-1", new DateTime(2030, 1, 14, 0, 0, 0, DateTimeKind.Utc));
        SeedDownloadHistory(userId, "vid-2", new DateTime(2030, 1, 13, 0, 0, 0, DateTimeKind.Utc));
        SeedDownloadHistory(userId, "vid-3", new DateTime(2030, 1, 12, 0, 0, 0, DateTimeKind.Utc));
        SeedDownloadHistory(otherUserId, "vid-theirs", new DateTime(2030, 1, 15, 0, 0, 0, DateTimeKind.Utc));

        var response = await client.GetAsync($"{BaseUrl}/downloads/history?limit=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DownloadHistoryResponse>();
        body!.Items.Select(i => i.VideoId).Should().Equal("vid-1", "vid-2");
    }

    [Fact]
    public async Task RejectsDownloadHistory_WhenLimitOutOfRange()
    {
        var userId = NextUserId();
        var client = _factory.CreateAuthenticatedClient(userId, Permissions.YtDownloaderAccess);

        var response = await client.GetAsync($"{BaseUrl}/downloads/history?limit=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RejectsDownloadHistory_WhenAnonymous()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{BaseUrl}/downloads/history");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RejectsDownloadHistory_WhenTokenLacksYtDownloaderAccess()
    {
        var userId = NextUserId();
        var client = _factory.CreateAuthenticatedClient(userId, Permissions.UsersManage);

        var response = await client.GetAsync($"{BaseUrl}/downloads/history");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ArchivesDownloadHistory_WhenOwnedByCaller()
    {
        var userId = NextUserId();
        var client = _factory.CreateAuthenticatedClient(userId, Permissions.YtDownloaderAccess);
        var entryId = SeedDownloadHistory(userId, "vid-1", new DateTime(2030, 1, 14, 0, 0, 0, DateTimeKind.Utc));

        var response = await client.PatchAsJsonAsync($"{BaseUrl}/downloads/history/{entryId}/archive", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.InFreshScope(db => db.YoutubeDownloadHistory.Find(entryId)!.IsArchived).Should().BeTrue();
    }

    [Fact]
    public async Task ReturnsNotFound_WhenArchivingAnotherUsersDownload()
    {
        var userId = NextUserId();
        var otherUserId = NextUserId();
        var client = _factory.CreateAuthenticatedClient(userId, Permissions.YtDownloaderAccess);
        var entryId = SeedDownloadHistory(otherUserId, "vid-1", new DateTime(2030, 1, 14, 0, 0, 0, DateTimeKind.Utc));

        var response = await client.PatchAsJsonAsync($"{BaseUrl}/downloads/history/{entryId}/archive", new { });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _factory.InFreshScope(db => db.YoutubeDownloadHistory.Find(entryId)!.IsArchived).Should().BeFalse();
    }

    [Fact]
    public async Task RejectsDownloadArchive_WhenAnonymous()
    {
        var client = _factory.CreateClient();

        var response = await client.PatchAsJsonAsync($"{BaseUrl}/downloads/history/1/archive", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReturnsCombinedHistory_ForCallerOnly()
    {
        var userId = NextUserId();
        var otherUserId = NextUserId();
        var client = _factory.CreateAuthenticatedClient(userId, Permissions.YtDownloaderAccess);
        SeedSearchHistory(userId, "my search", new DateTime(2030, 1, 14, 0, 0, 0, DateTimeKind.Utc));
        SeedDownloadHistory(userId, "vid-1", new DateTime(2030, 1, 14, 0, 0, 0, DateTimeKind.Utc));
        SeedSearchHistory(otherUserId, "their search", new DateTime(2030, 1, 15, 0, 0, 0, DateTimeKind.Utc));
        SeedDownloadHistory(otherUserId, "vid-theirs", new DateTime(2030, 1, 15, 0, 0, 0, DateTimeKind.Utc));

        var response = await client.GetAsync($"{BaseUrl}/history");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetHistoryResponse>();
        body!.Searches.Select(s => s.SearchText).Should().Equal("my search");
        body.Downloads.Select(d => d.VideoId).Should().Equal("vid-1");
    }

    [Fact]
    public async Task RejectsHistory_WhenAnonymous()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{BaseUrl}/history");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReportsHealthy_WithExpectedShape()
    {
        // /health is mapped outside the authenticated /api/v1/yt group, so it is
        // intentionally unauthenticated — the anonymous client must still get 200.
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{BaseUrl}/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("healthy");
        body.Database.Should().Be("connected");
        body.Timestamp.Should().NotBe(default);
    }

    private long SeedDownloadHistory(long userId, string videoId, DateTime downloadedAt) =>
        _factory.InFreshScope(db =>
        {
            var entry = new YoutubeDownloadHistory
            {
                UserId = userId,
                VideoId = videoId,
                VideoTitle = $"Title {videoId}",
                VideoUrl = $"https://youtube.com/watch?v={videoId}",
                DownloadedAt = downloadedAt,
            };
            db.YoutubeDownloadHistory.Add(entry);
            db.SaveChanges();
            return entry.Id;
        });

    private long SeedSearchHistory(long userId, string searchText, DateTime searchedAt) =>
        _factory.InFreshScope(db =>
        {
            var entry = new YoutubeSearchHistory
            {
                UserId = userId,
                SearchText = searchText,
                SearchedAt = searchedAt,
            };
            db.YoutubeSearchHistory.Add(entry);
            db.SaveChanges();
            return entry.Id;
        });

    // Seeds a Completed DownloadCommand row pointing at an existing file, the same
    // shape the store's GetFilePath serves — no real download ever runs.
    private string SeedReadyJob(long userId, string filePath)
    {
        var tempId = Guid.NewGuid().ToString("N");
        var now = new DateTime(2030, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        _factory.InFreshScope(db =>
        {
            var command = DownloadCommand.Create(
                tempId, userId, VideoId, "Title", null, null,
                $"https://youtube.com/watch?v={VideoId}", now, TimeSpan.FromMinutes(15));
            command.MarkReady(filePath, now, TimeSpan.FromMinutes(15));
            db.DownloadCommands.Add(command);
            db.SaveChanges();
            return true;
        });
        return tempId;
    }

    private sealed record HealthResponse(string Status, string Database, DateTime Timestamp);

    private static long NextUserId() => Interlocked.Increment(ref _userIdCounter);
}
