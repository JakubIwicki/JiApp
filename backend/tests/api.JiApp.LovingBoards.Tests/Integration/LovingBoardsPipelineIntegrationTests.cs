using System.Net;
using System.Net.Http.Json;
using api.JiApp.LovingBoards.Features.Boards.CreateBoard;
using api.JiApp.LovingBoards.Features.Boards.GetBoard;
using api.JiApp.LovingBoards.Features.Items.CreateItem;
using api.JiApp.LovingBoards.Features.Items.SetItemStatus;
using JiApp.Common.Constants;

namespace api.JiApp.LovingBoards.Tests.Integration;

/// <summary>
/// Tier A full-pipeline suite for the LovingBoards module: real HTTP through the
/// host's routing → auth middleware → endpoint filters → handler → real SQLite
/// store, with the remote security-stamp recheck and user-existence probe doubled
/// (NoOp). Isolation is by unique userId per test — no per-test DB reset.
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

    private async Task<(long OwnerId, long BoardId)> CreateBoard(string name)
    {
        var ownerId = NextUserId();
        var client = _factory.CreateAuthenticatedClient(ownerId, Permissions.LovingBoardsAccess);

        var response = await client.PostAsJsonAsync($"{BaseUrl}/boards", new CreateBoardRequest(name));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var boardId = (await response.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        return (ownerId, boardId);
    }

    private static long NextUserId() => Interlocked.Increment(ref _userIdCounter);

    private sealed record IdResponse(long Id);
}
