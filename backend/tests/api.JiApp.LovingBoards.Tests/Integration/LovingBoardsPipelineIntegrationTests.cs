using System.Net;
using System.Net.Http.Json;
using System.Text;
using api.JiApp.LovingBoards.Features.Boards.AddBoardMember;
using api.JiApp.LovingBoards.Features.Boards.CreateBoard;
using api.JiApp.LovingBoards.Features.Boards.GetBoard;
using api.JiApp.LovingBoards.Features.Boards.ListBoards;
using api.JiApp.LovingBoards.Features.Boards.UpdateBoard;
using api.JiApp.LovingBoards.Features.Items.CreateItem;
using api.JiApp.LovingBoards.Features.Items.SetItemStatus;
using JiApp.Common.Constants;

namespace api.JiApp.LovingBoards.Tests.Integration;

/// <summary>
/// Tier A full-pipeline suite for the LovingBoards module: real HTTP through the
/// host's routing → auth middleware → endpoint filters → handler → real SQLite
/// store, with the remote security-stamp recheck and user-existence probe doubled
/// (NoOp). Isolation is by unique userId per test — no per-test DB reset.
/// Also drives the SSE stream route end-to-end against the real BoardBroadcaster.
/// </summary>
public sealed class LovingBoardsPipelineIntegrationTests : IClassFixture<LovingBoardsPipelineWebApplicationFactory>
{
    private const string BaseUrl = "/api/v1/lovingboards";
    private static int _userIdCounter;

    private readonly LovingBoardsPipelineWebApplicationFactory _factory;

    public LovingBoardsPipelineIntegrationTests(LovingBoardsPipelineWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task RejectsBoardCreate_WhenAnonymous()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync($"{BaseUrl}/boards", new CreateBoardRequest("Board"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RejectsBoardCreate_WhenTokenLacksLovingBoardsAccess()
    {
        var userId = NextUserId();
        var client = _factory.CreateAuthenticatedClient(userId, Permissions.UsersManage);

        var response = await client.PostAsJsonAsync($"{BaseUrl}/boards", new CreateBoardRequest("Board"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _factory.InFreshScope(db => db.Boards.Count(b => b.OwnerUserId == userId)).Should().Be(0);
    }

    [Fact]
    public async Task PersistsBoard_WhenOwnerCreatesBoard()
    {
        var userId = NextUserId();
        var client = _factory.CreateAuthenticatedClient(userId, Permissions.LovingBoardsAccess);

        var response = await client.PostAsJsonAsync($"{BaseUrl}/boards", new CreateBoardRequest("My Board"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var boardId = (await response.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var board = _factory.InFreshScope(db => db.Boards.Find(boardId));
        board.Should().NotBeNull();
        board!.OwnerUserId.Should().Be(userId);
    }

    [Fact]
    public async Task ReturnsBoardWithItems_WhenMemberReads()
    {
        var (ownerId, boardId) = await CreateBoard("My Board");
        var client = _factory.CreateAuthenticatedClient(ownerId, Permissions.LovingBoardsAccess);

        var createItemResponse = await client.PostAsJsonAsync(
            $"{BaseUrl}/boards/{boardId}/items", new CreateItemRequest("Milk", "2", "Groceries"));
        createItemResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var itemId = (await createItemResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var response = await client.GetAsync($"{BaseUrl}/boards/{boardId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetBoardResponse>();
        body.Should().NotBeNull();
        body!.Items.Should().ContainSingle(i => i.Id == itemId && i.Title == "Milk");
    }

    [Fact]
    public async Task ReturnsNotFound_WhenNonMemberReadsBoard()
    {
        var (_, boardId) = await CreateBoard("My Board");
        var other = _factory.CreateAuthenticatedClient(NextUserId(), Permissions.LovingBoardsAccess);

        var response = await other.GetAsync($"{BaseUrl}/boards/{boardId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenNonMemberCreatesItem()
    {
        var (_, boardId) = await CreateBoard("My Board");
        var other = _factory.CreateAuthenticatedClient(NextUserId(), Permissions.LovingBoardsAccess);

        var response = await other.PostAsJsonAsync($"{BaseUrl}/boards/{boardId}/items", new CreateItemRequest("Sneaky"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        _factory.InFreshScope(db => db.BoardItems.Count(i => i.BoardId == boardId)).Should().Be(0);
    }

    [Fact]
    public async Task PersistsItemStatus_WhenStatusSet()
    {
        var (ownerId, boardId) = await CreateBoard("My Board");
        var client = _factory.CreateAuthenticatedClient(ownerId, Permissions.LovingBoardsAccess);

        var createItemResponse = await client.PostAsJsonAsync(
            $"{BaseUrl}/boards/{boardId}/items", new CreateItemRequest("Milk"));
        createItemResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var itemId = (await createItemResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var response = await client.PutAsJsonAsync(
            $"{BaseUrl}/boards/{boardId}/items/{itemId}/status", new SetItemStatusRequest("Completed"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var item = _factory.InFreshScope(db => db.BoardItems.Find(itemId));
        item.Should().NotBeNull();
        item!.Status.Should().Be(BoardItemStatus.Completed);
    }

    [Fact]
    public async Task DeletesBoard_WhenOwnerDeletes()
    {
        var (ownerId, boardId) = await CreateBoard("My Board");
        var client = _factory.CreateAuthenticatedClient(ownerId, Permissions.LovingBoardsAccess);

        var createItemResponse = await client.PostAsJsonAsync(
            $"{BaseUrl}/boards/{boardId}/items", new CreateItemRequest("Milk"));
        createItemResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await client.DeleteAsync($"{BaseUrl}/boards/{boardId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        _factory.InFreshScope(db => db.Boards.Find(boardId)).Should().BeNull();
        _factory.InFreshScope(db => db.BoardItems.Count(i => i.BoardId == boardId)).Should().Be(0);
    }

    [Fact]
    public async Task PersistsBoardName_WhenOwnerUpdatesBoard()
    {
        var (ownerId, boardId) = await CreateBoard("Original");
        var client = _factory.CreateAuthenticatedClient(ownerId, Permissions.LovingBoardsAccess);

        var response = await client.PutAsJsonAsync($"{BaseUrl}/boards/{boardId}", new UpdateBoardRequest("Renamed"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.InFreshScope(db => db.Boards.Find(boardId)!.Name).Should().Be("Renamed");
    }

    [Fact]
    public async Task ReturnsNotFound_WhenNonMemberUpdatesBoard()
    {
        var (_, boardId) = await CreateBoard("Original");
        var other = _factory.CreateAuthenticatedClient(NextUserId(), Permissions.LovingBoardsAccess);

        var response = await other.PutAsJsonAsync($"{BaseUrl}/boards/{boardId}", new UpdateBoardRequest("Hijacked"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _factory.InFreshScope(db => db.Boards.Find(boardId)!.Name).Should().Be("Original");
    }

    [Fact]
    public async Task ListsBoards_ForCurrentUser()
    {
        var (ownerId, boardId) = await CreateBoard("My Board");
        var client = _factory.CreateAuthenticatedClient(ownerId, Permissions.LovingBoardsAccess);

        var response = await client.GetAsync($"{BaseUrl}/boards");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListBoardsResponse>();
        body.Should().NotBeNull();
        body!.Boards.Should().ContainSingle(b => b.Id == boardId && b.Name == "My Board");
        body.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task ListsOnlyBoards_CurrentUserIsMemberOf()
    {
        var (ownerId, boardId) = await CreateBoard("Mine");
        var (_, otherBoardId) = await CreateBoard("Theirs");
        var client = _factory.CreateAuthenticatedClient(ownerId, Permissions.LovingBoardsAccess);

        var response = await client.GetAsync($"{BaseUrl}/boards");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListBoardsResponse>();
        body.Should().NotBeNull();
        body!.Boards.Select(b => b.Id).Should().Contain(boardId);
        body.Boards.Select(b => b.Id).Should().NotContain(otherBoardId);
        _factory.InFreshScope(db => db.Boards.Find(otherBoardId)).Should().NotBeNull();
    }

    [Fact]
    public async Task AddsMember_WhenOwnerInvitesUser()
    {
        var (ownerId, boardId) = await CreateBoard("My Board");
        var client = _factory.CreateAuthenticatedClient(ownerId, Permissions.LovingBoardsAccess);
        var newMember = NextUserId();

        var response = await client.PostAsJsonAsync(
            $"{BaseUrl}/boards/{boardId}/members", new AddBoardMemberRequest(newMember));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        _factory.InFreshScope(db => db.Boards.Find(boardId)!.MemberUserIds).Should().Contain(newMember);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenNonOwnerAddsMember()
    {
        var (_, boardId) = await CreateBoard("My Board");
        var other = _factory.CreateAuthenticatedClient(NextUserId(), Permissions.LovingBoardsAccess);
        var wouldBeMember = NextUserId();

        var response = await other.PostAsJsonAsync(
            $"{BaseUrl}/boards/{boardId}/members", new AddBoardMemberRequest(wouldBeMember));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _factory.InFreshScope(db => db.Boards.Find(boardId)!.MemberUserIds).Should().NotContain(wouldBeMember);
    }

    [Fact]
    public async Task RemovesMember_WhenOwnerRemovesUser()
    {
        var (ownerId, boardId) = await CreateBoard("My Board");
        var owner = _factory.CreateAuthenticatedClient(ownerId, Permissions.LovingBoardsAccess);
        var member = NextUserId();
        await owner.PostAsJsonAsync($"{BaseUrl}/boards/{boardId}/members", new AddBoardMemberRequest(member));

        var response = await owner.DeleteAsync($"{BaseUrl}/boards/{boardId}/members/{member}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.InFreshScope(db => db.Boards.Find(boardId)!.MemberUserIds).Should().NotContain(member);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenNonOwnerRemovesMember()
    {
        var (ownerId, boardId) = await CreateBoard("My Board");
        var owner = _factory.CreateAuthenticatedClient(ownerId, Permissions.LovingBoardsAccess);
        var member = NextUserId();
        await owner.PostAsJsonAsync($"{BaseUrl}/boards/{boardId}/members", new AddBoardMemberRequest(member));
        var other = _factory.CreateAuthenticatedClient(NextUserId(), Permissions.LovingBoardsAccess);

        var response = await other.DeleteAsync($"{BaseUrl}/boards/{boardId}/members/{member}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _factory.InFreshScope(db => db.Boards.Find(boardId)!.MemberUserIds).Should().Contain(member);
    }

    [Fact]
    public async Task PersistsItemTitle_WhenMemberUpdatesItem()
    {
        var (ownerId, boardId) = await CreateBoard("My Board");
        var client = _factory.CreateAuthenticatedClient(ownerId, Permissions.LovingBoardsAccess);
        var itemId = await CreateItem(client, boardId, "Milk");

        // UpdateItemRequest is an inbound-only PATCH contract — unset Optional fields cannot be
        // serialized, so the request body is sent as raw JSON naming only the field being patched.
        var response = await client.PutAsync(
            $"{BaseUrl}/boards/{boardId}/items/{itemId}",
            new StringContent("""{"title": "Milk 2L"}""", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.InFreshScope(db => db.BoardItems.Find(itemId)!.Title).Should().Be("Milk 2L");
    }

    [Fact]
    public async Task ReturnsNotFound_WhenNonMemberUpdatesItem()
    {
        var (ownerId, boardId) = await CreateBoard("My Board");
        var owner = _factory.CreateAuthenticatedClient(ownerId, Permissions.LovingBoardsAccess);
        var itemId = await CreateItem(owner, boardId, "Milk");
        var other = _factory.CreateAuthenticatedClient(NextUserId(), Permissions.LovingBoardsAccess);

        var response = await other.PutAsync(
            $"{BaseUrl}/boards/{boardId}/items/{itemId}",
            new StringContent("""{"title": "Hijacked"}""", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _factory.InFreshScope(db => db.BoardItems.Find(itemId)!.Title).Should().Be("Milk");
    }

    [Fact]
    public async Task DeletesItem_WhenMemberDeletes()
    {
        var (ownerId, boardId) = await CreateBoard("My Board");
        var client = _factory.CreateAuthenticatedClient(ownerId, Permissions.LovingBoardsAccess);
        var itemId = await CreateItem(client, boardId, "Milk");

        var response = await client.DeleteAsync($"{BaseUrl}/boards/{boardId}/items/{itemId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.InFreshScope(db => db.BoardItems.Find(itemId)).Should().BeNull();
    }

    [Fact]
    public async Task ReturnsNotFound_WhenNonMemberDeletesItem()
    {
        var (ownerId, boardId) = await CreateBoard("My Board");
        var owner = _factory.CreateAuthenticatedClient(ownerId, Permissions.LovingBoardsAccess);
        var itemId = await CreateItem(owner, boardId, "Milk");
        var other = _factory.CreateAuthenticatedClient(NextUserId(), Permissions.LovingBoardsAccess);

        var response = await other.DeleteAsync($"{BaseUrl}/boards/{boardId}/items/{itemId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _factory.InFreshScope(db => db.BoardItems.Find(itemId)).Should().NotBeNull();
    }

    [Fact]
    public async Task SoftRemovesCompletedItems_WhenMemberRequestsClear()
    {
        var (ownerId, boardId, itemId) = await CreateBoardWithItemInStatus("Milk", "Completed");
        var client = _factory.CreateAuthenticatedClient(ownerId, Permissions.LovingBoardsAccess);

        var response = await client.PostAsync($"{BaseUrl}/boards/{boardId}/items/clear-completed", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ClearedResponse>();
        body!.Cleared.Should().Be(1);
        _factory.InFreshScope(db => db.BoardItems.Find(itemId)!.Status).Should().Be(BoardItemStatus.Removed);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenNonMemberClearsCompleted()
    {
        var (_, boardId, itemId) = await CreateBoardWithItemInStatus("Milk", "Completed");
        var other = _factory.CreateAuthenticatedClient(NextUserId(), Permissions.LovingBoardsAccess);

        var response = await other.PostAsync($"{BaseUrl}/boards/{boardId}/items/clear-completed", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _factory.InFreshScope(db => db.BoardItems.Find(itemId)!.Status).Should().Be(BoardItemStatus.Completed);
    }

    [Fact]
    public async Task ResetsRecurringItems_WhenMemberRequestsReset()
    {
        var (ownerId, boardId, itemId) = await CreateBoardWithItemInStatus("Milk", "Completed", isRecurring: true);
        var client = _factory.CreateAuthenticatedClient(ownerId, Permissions.LovingBoardsAccess);

        var response = await client.PostAsync($"{BaseUrl}/boards/{boardId}/items/reset-weekly", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ResetResponse>();
        body!.Reset.Should().Be(1);
        _factory.InFreshScope(db => db.BoardItems.Find(itemId)!.Status).Should().Be(BoardItemStatus.Needed);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenNonMemberResetsWeekly()
    {
        var (_, boardId, itemId) = await CreateBoardWithItemInStatus("Milk", "Completed", isRecurring: true);
        var other = _factory.CreateAuthenticatedClient(NextUserId(), Permissions.LovingBoardsAccess);

        var response = await other.PostAsync($"{BaseUrl}/boards/{boardId}/items/reset-weekly", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _factory.InFreshScope(db => db.BoardItems.Find(itemId)!.Status).Should().Be(BoardItemStatus.Completed);
    }

    [Fact]
    public async Task ReportsHealthy_WithExpectedShape()
    {
        // /health is mapped outside the permission-gated /api/v1/lovingboards group, so it
        // is intentionally unauthenticated — the anonymous client must still get 200.
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{BaseUrl}/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("healthy");
        body.Database.Should().Be("connected");
        body.Timestamp.Should().NotBe(default);
    }

    [Fact]
    public async Task StreamsSseHeadersAndKeepAlive_WhenMemberConnects()
    {
        var (ownerId, boardId) = await CreateBoard("SSE Board");
        var member = await AddMember(ownerId, boardId);
        var memberClient = _factory.CreateAuthenticatedClient(member, Permissions.LovingBoardsAccess);

        using var response = await memberClient.GetAsync(
            $"{BaseUrl}/boards/{boardId}/stream", HttpCompletionOption.ResponseHeadersRead);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");
        response.Headers.CacheControl!.NoCache.Should().BeTrue();
        GetHeader(response, "X-Accel-Buffering").Should().Contain("no");

        var stream = await response.Content.ReadAsStreamAsync();
        var body = await ReadUntilMarkerAsync(stream, ": keep-alive", TimeSpan.FromSeconds(5));

        body.Should().Contain(": keep-alive");
    }

    [Fact]
    public async Task DeliversItemAddedEvent_OnOpenStream_WhenMemberCreatesItem()
    {
        var (ownerId, boardId) = await CreateBoard("SSE Board");
        var member = await AddMember(ownerId, boardId);
        var owner = _factory.CreateAuthenticatedClient(ownerId, Permissions.LovingBoardsAccess);
        var memberClient = _factory.CreateAuthenticatedClient(member, Permissions.LovingBoardsAccess);

        using var response = await memberClient.GetAsync(
            $"{BaseUrl}/boards/{boardId}/stream", HttpCompletionOption.ResponseHeadersRead);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stream = await response.Content.ReadAsStreamAsync();

        await ReadUntilMarkerAsync(stream, ": keep-alive", TimeSpan.FromSeconds(5));

        var createResponse = await owner.PostAsJsonAsync($"{BaseUrl}/boards/{boardId}/items", new CreateItemRequest("Milk"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await ReadUntilMarkerAsync(stream, "item.added", TimeSpan.FromSeconds(5));

        body.Should().Contain("event: item.added");
        body.Should().Contain("data: {");
    }

    [Fact]
    public async Task ReturnsNotFound_WhenNonMemberConnectsToStream()
    {
        var (_, boardId) = await CreateBoard("SSE Board");
        var stranger = _factory.CreateAuthenticatedClient(NextUserId(), Permissions.LovingBoardsAccess);

        using var response = await stranger.GetAsync(
            $"{BaseUrl}/boards/{boardId}/stream", HttpCompletionOption.ResponseHeadersRead);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().NotBe("text/event-stream");
    }

    [Fact]
    public async Task EndsMembersStream_WhenOwnerRemovesMember()
    {
        var (ownerId, boardId) = await CreateBoard("SSE Board");
        var member = await AddMember(ownerId, boardId);
        var owner = _factory.CreateAuthenticatedClient(ownerId, Permissions.LovingBoardsAccess);
        var memberClient = _factory.CreateAuthenticatedClient(member, Permissions.LovingBoardsAccess);

        using var response = await memberClient.GetAsync(
            $"{BaseUrl}/boards/{boardId}/stream", HttpCompletionOption.ResponseHeadersRead);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stream = await response.Content.ReadAsStreamAsync();

        await ReadUntilMarkerAsync(stream, ": keep-alive", TimeSpan.FromSeconds(5));

        var removeResponse = await owner.DeleteAsync($"{BaseUrl}/boards/{boardId}/members/{member}");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // The removed member's stream must complete (EOF) within the bound — a regression
        // that left the channel open would throw TimeoutException instead of hanging the suite.
        var tail = await ReadUntilEndAsync(stream, TimeSpan.FromSeconds(5));

        tail.Should().Contain("member.changed");
    }

    private async Task<(long OwnerId, long BoardId)> CreateBoard(string name)
    {
        var ownerId = NextUserId();
        var client = _factory.CreateAuthenticatedClient(ownerId, Permissions.LovingBoardsAccess);

        var response = await client.PostAsJsonAsync($"{BaseUrl}/boards", new CreateBoardRequest(name));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var boardId = (await response.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        return (ownerId, boardId);
    }

    private async Task<long> CreateItem(HttpClient client, long boardId, string title, bool isRecurring = false)
    {
        var response = await client.PostAsJsonAsync(
            $"{BaseUrl}/boards/{boardId}/items", new CreateItemRequest(title, IsRecurring: isRecurring));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<IdResponse>())!.Id;
    }

    private async Task<(long OwnerId, long BoardId, long ItemId)> CreateBoardWithItemInStatus(
        string title, string status, bool isRecurring = false)
    {
        var (ownerId, boardId) = await CreateBoard("My Board");
        var client = _factory.CreateAuthenticatedClient(ownerId, Permissions.LovingBoardsAccess);

        var itemId = await CreateItem(client, boardId, title, isRecurring);

        var statusResponse = await client.PutAsJsonAsync(
            $"{BaseUrl}/boards/{boardId}/items/{itemId}/status", new SetItemStatusRequest(status));
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        return (ownerId, boardId, itemId);
    }

    private async Task<long> AddMember(long ownerId, long boardId)
    {
        var member = NextUserId();
        var owner = _factory.CreateAuthenticatedClient(ownerId, Permissions.LovingBoardsAccess);

        var response = await owner.PostAsJsonAsync($"{BaseUrl}/boards/{boardId}/members", new AddBoardMemberRequest(member));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return member;
    }

    private static long NextUserId() => Interlocked.Increment(ref _userIdCounter);

    private static async Task<string> ReadUntilMarkerAsync(Stream stream, string marker, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        var buffer = new byte[4096];
        var sb = new StringBuilder();

        while (true)
        {
            int n;
            try
            {
                n = await stream.ReadAsync(buffer, cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"The stream never produced '{marker}' within {timeout}.");
            }

            if (n == 0)
                return sb.ToString();

            sb.Append(Encoding.UTF8.GetString(buffer, 0, n));

            if (sb.ToString().Contains(marker))
                return sb.ToString();
        }
    }

    private static async Task<string> ReadUntilEndAsync(Stream stream, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        var buffer = new byte[4096];
        var sb = new StringBuilder();

        while (true)
        {
            int n;
            try
            {
                n = await stream.ReadAsync(buffer, cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"The stream did not terminate within {timeout}.");
            }

            if (n == 0)
                return sb.ToString();

            sb.Append(Encoding.UTF8.GetString(buffer, 0, n));
        }
    }

    private static IEnumerable<string> GetHeader(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values)
            ? values
            : response.Content.Headers.GetValues(name);

    private sealed record IdResponse(long Id);
    private sealed record ClearedResponse(int Cleared);
    private sealed record ResetResponse(int Reset);
    private sealed record HealthResponse(string Status, string Database, DateTime Timestamp);
}
