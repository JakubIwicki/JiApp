using System.Net;
using System.Net.Http.Json;
using JiApp.Common.Constants;
using JiApp.Scheduler.Features.Appointments.CreateAppointment;
using JiApp.Scheduler.Features.Boards.AddBoardMember;
using JiApp.Scheduler.Features.Boards.CreateBoard;
using JiApp.Scheduler.Features.Boards.UpdateBoard;
using JiApp.Scheduler.Features.Clients.CreateClient;
using JiApp.Scheduler.Features.Common;
using JiApp.Scheduler.Features.Services.CreateService;

namespace JiApp.Scheduler.Tests.Integration;

/// <summary>
/// Tier A full-pipeline suite for the Scheduler module: real HTTP through the host's
/// routing → auth middleware → endpoint filters → handler → real SQLite store, with
/// only the remote security-stamp recheck doubled (NoOp). Isolation is by unique
/// userId per test — no per-test DB reset — so every board/client/appointment row
/// belongs to a single test's identity.
/// </summary>
public sealed class SchedulerPipelineIntegrationTests : IClassFixture<SchedulerPipelineWebApplicationFactory>
{
    private const string BaseUrl = "/api/v1/scheduler";
    private static int _userIdCounter;

    private readonly SchedulerPipelineWebApplicationFactory _factory;

    public SchedulerPipelineIntegrationTests(SchedulerPipelineWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task RejectsBoardCreate_WhenAnonymous()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync($"{BaseUrl}/boards", new CreateBoardRequest("Board"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RejectsBoardCreate_WhenTokenLacksSchedulerAccess()
    {
        var client = _factory.CreateAuthenticatedClient(NextUserId(), Permissions.UsersManage);

        var response = await client.PostAsJsonAsync($"{BaseUrl}/boards", new CreateBoardRequest("Board"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PersistsBoard_WhenOwnerCreatesBoard()
    {
        var userId = NextUserId();
        var client = _factory.CreateAuthenticatedClient(userId, Permissions.SchedulerAccess);

        var response = await client.PostAsJsonAsync($"{BaseUrl}/boards", new CreateBoardRequest("My Board"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var boardId = (await response.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var board = _factory.InFreshScope(db => db.Boards.Find(boardId));
        board.Should().NotBeNull();
        board!.OwnerUserId.Should().Be(userId);
        board.MemberUserIds.Should().Contain(userId);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenNonMemberReadsBoard()
    {
        var (_, boardId) = await CreateBoard("A's Board");
        var other = _factory.CreateAuthenticatedClient(NextUserId(), Permissions.SchedulerAccess);

        var response = await other.GetAsync($"{BaseUrl}/boards/{boardId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenNonMemberUpdatesBoard()
    {
        var (_, boardId) = await CreateBoard("A's Board");
        var other = _factory.CreateAuthenticatedClient(NextUserId(), Permissions.SchedulerAccess);

        var response = await other.PutAsJsonAsync($"{BaseUrl}/boards/{boardId}", new UpdateBoardRequest("Renamed"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var board = _factory.InFreshScope(db => db.Boards.Find(boardId));
        board.Should().NotBeNull();
        board!.Name.Should().Be("A's Board");
    }

    [Fact]
    public async Task ReturnsNotFound_WhenNonMemberDeletesBoard()
    {
        var (ownerId, boardId) = await CreateBoard("A's Board");
        var other = _factory.CreateAuthenticatedClient(NextUserId(), Permissions.SchedulerAccess);

        var response = await other.DeleteAsync($"{BaseUrl}/boards/{boardId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var board = _factory.InFreshScope(db => db.Boards.Find(boardId));
        board.Should().NotBeNull();
        board!.OwnerUserId.Should().Be(ownerId);
    }

    [Fact]
    public async Task PersistsMember_WhenOwnerAddsBoardMember()
    {
        var (ownerId, boardId) = await CreateBoard("A's Board");
        var memberId = NextUserId();
        var client = _factory.CreateAuthenticatedClient(ownerId, Permissions.SchedulerAccess);

        var response = await client.PostAsJsonAsync(
            $"{BaseUrl}/boards/{boardId}/members", new AddBoardMemberRequest(memberId));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var board = _factory.InFreshScope(db => db.Boards.Find(boardId));
        board.Should().NotBeNull();
        board!.MemberUserIds.Should().Contain(memberId);
    }

    [Fact]
    public async Task PersistsClientAndAppointment_WhenCreated()
    {
        var (ownerId, boardId) = await CreateBoard("Beauty Board");
        var client = _factory.CreateAuthenticatedClient(ownerId, Permissions.SchedulerAccess);

        var createClientResponse = await client.PostAsJsonAsync(
            $"{BaseUrl}/clients", new CreateClientRequest(boardId, "Alice", null, null));
        createClientResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var clientId = (await createClientResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var createServiceResponse = await client.PostAsJsonAsync(
            $"{BaseUrl}/services",
            new CreateServiceRequest(boardId, "Haircut", "MensHaircut", 30, new PriceRequest(100)));
        createServiceResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var serviceId = (await createServiceResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var createAppointmentResponse = await client.PostAsJsonAsync(
            $"{BaseUrl}/appointments",
            new CreateAppointmentRequest(
                boardId, clientId, serviceId,
                new DateOnly(2026, 8, 15) /* Saturday — appointments require a weekend day */, new TimeOnly(10, 0), new TimeOnly(11, 0),
                null, "Room 1", new PriceRequest(100)));
        createAppointmentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var appointmentId = (await createAppointmentResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        _factory.InFreshScope(db => db.Clients.Find(clientId)).Should().NotBeNull();
        _factory.InFreshScope(db => db.Services.Find(serviceId)).Should().NotBeNull();
        _factory.InFreshScope(db => db.Appointments.Find(appointmentId)).Should().NotBeNull();
    }

    private async Task<(long OwnerId, long BoardId)> CreateBoard(string name)
    {
        var ownerId = NextUserId();
        var client = _factory.CreateAuthenticatedClient(ownerId, Permissions.SchedulerAccess);

        var response = await client.PostAsJsonAsync($"{BaseUrl}/boards", new CreateBoardRequest(name));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var boardId = (await response.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        return (ownerId, boardId);
    }

    private static long NextUserId() => Interlocked.Increment(ref _userIdCounter);

    private sealed record IdResponse(long Id);
}
