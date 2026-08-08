using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using JiApp.Common.Constants;
using JiApp.Identity.Features.Auth.Login;
using JiApp.Identity.Features.Auth.Register;
using JiApp.Scheduler.Features.Boards.AddBoardMember;
using JiApp.Scheduler.Features.Boards.CreateBoard;
using JiApp.Testing.Common.Auth;

namespace JiApp.Gateway.Tests.Integration.CrossModule;

/// <summary>
/// Tier B — cross-module Gateway E2E: one real HTTP request enters the Gateway's
/// TestServer, is reverse-proxied over a real socket to a module host, and the
/// module's own cross-host clients (RemoteSecurityStampValidator, UserExistenceClient)
/// make genuine HTTP round trips to the real Identity socket. Every host runs its
/// real pipeline; only the module stores are ephemeral in-memory SQLite.
/// </summary>
public sealed class GatewayCrossModuleE2ETests : IClassFixture<GatewayCrossModuleFixture>
{
    private const string RegisterUrl = "/api/v1/auth/register";
    private const string LoginUrl = "/api/v1/auth/login";
    private const string MeUrl = "/api/v1/auth/me";
    private const string SchedulerBoardsUrl = "/api/v1/scheduler/boards";
    private const string LovingBoardsBoardsUrl = "/api/v1/lovingboards/boards";

    private readonly GatewayCrossModuleFixture _fixture;

    public GatewayCrossModuleE2ETests(GatewayCrossModuleFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ReturnsTokens_WhenRegisteringAndLoggingIn_ThroughGateway()
    {
        var credentials = NewCredentials();

        var registerResponse = await _fixture.GatewayClient.PostAsJsonAsync(RegisterUrl, credentials.Request);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginResponse = await _fixture.GatewayClient.PostAsJsonAsync(
            LoginUrl, new LoginRequest(credentials.Username, credentials.Password));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        login.Should().NotBeNull();
        login!.AccessToken.Should().NotBeNullOrWhiteSpace();
        login.Roles.Should().Contain("Guest");
        login.Permissions.Should().BeEmpty();
    }

    [Fact]
    public async Task ReturnsCurrentUser_WhenMeRequested_WithLoginToken_ThroughGateway()
    {
        var credentials = NewCredentials();
        var accessToken = await RegisterAndLogin(credentials);

        var meClient = CreateAuthenticatedClient(accessToken);
        var meResponse = await meClient.GetAsync(MeUrl);

        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReturnsCreated_WhenAuthorizedSchedulerCall_ThroughGateway_WithRealStampValidation()
    {
        var credentials = NewCredentials();
        var userId = await RegisterUser(credentials);
        var securityStamp = ReadSecurityStamp(userId);
        securityStamp.Should().NotBeNullOrWhiteSpace();
        var token = TestTokens.Mint(userId, permissions: [Permissions.SchedulerAccess], securityStamp: securityStamp);
        var client = CreateAuthenticatedClient(token);

        var createBoardResponse = await client.PostAsJsonAsync(SchedulerBoardsUrl, new CreateBoardRequest("Cross Module Board"));
        createBoardResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var boardId = (await createBoardResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var board = _fixture.Scheduler.InFreshScope(db => db.Boards.Find(boardId));
        board.Should().NotBeNull();
        board!.OwnerUserId.Should().Be(userId);

        var memberId = 9001L;
        var addMemberResponse = await client.PostAsJsonAsync(
            $"{SchedulerBoardsUrl}/{boardId}/members", new AddBoardMemberRequest(memberId));
        addMemberResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var boardWithMember = _fixture.Scheduler.InFreshScope(db => db.Boards.Find(boardId));
        boardWithMember.Should().NotBeNull();
        boardWithMember!.MemberUserIds.Should().Contain(memberId);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenAnonymous_ThroughGateway()
    {
        var response = await _fixture.GatewayClient.PostAsJsonAsync(
            SchedulerBoardsUrl, new CreateBoardRequest("No Auth"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenCrossUserReadsBoard_ThroughGateway()
    {
        var ownerCredentials = NewCredentials();
        var ownerId = await RegisterUser(ownerCredentials);
        var ownerToken = TestTokens.Mint(
            ownerId, permissions: [Permissions.SchedulerAccess], securityStamp: ReadSecurityStamp(ownerId));
        var ownerClient = CreateAuthenticatedClient(ownerToken);

        var createResponse = await ownerClient.PostAsJsonAsync(SchedulerBoardsUrl, new CreateBoardRequest("A's Board"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var boardId = (await createResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var otherId = 424242L;
        var otherToken = TestTokens.Mint(otherId, permissions: [Permissions.SchedulerAccess]);
        var otherClient = CreateAuthenticatedClient(otherToken);

        var readResponse = await otherClient.GetAsync($"{SchedulerBoardsUrl}/{boardId}");

        readResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var board = _fixture.Scheduler.InFreshScope(db => db.Boards.Find(boardId));
        board.Should().NotBeNull();
        board!.OwnerUserId.Should().Be(ownerId);
    }

    [Fact]
    public async Task ReturnsCreated_WhenMemberAdded_WithRealUserExistenceProbe()
    {
        var xCredentials = NewCredentials();
        var xId = await RegisterUser(xCredentials);

        var ownerCredentials = NewCredentials();
        var ownerId = await RegisterUser(ownerCredentials);
        var ownerToken = TestTokens.Mint(
            ownerId, permissions: [Permissions.LovingBoardsAccess], securityStamp: ReadSecurityStamp(ownerId));
        var ownerClient = CreateAuthenticatedClient(ownerToken);

        var createBoardResponse = await ownerClient.PostAsJsonAsync(LovingBoardsBoardsUrl, new CreateBoardRequest("Shared Board"));
        createBoardResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var boardId = (await createBoardResponse.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var addMemberResponse = await ownerClient.PostAsJsonAsync(
            $"{LovingBoardsBoardsUrl}/{boardId}/members",
            new api.JiApp.LovingBoards.Features.Boards.AddBoardMember.AddBoardMemberRequest(xId));
        addMemberResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var board = _fixture.LovingBoards.InFreshScope(db => db.Boards.Find(boardId));
        board.Should().NotBeNull();
        board!.MemberUserIds.Should().Contain(xId);
    }

    private string ReadSecurityStamp(long userId) =>
        _fixture.Identity.InFreshScope(db => db.Users.Find(userId))!.SecurityStamp!;

    private HttpClient CreateAuthenticatedClient(string accessToken)
    {
        var client = _fixture.Gateway.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private async Task<long> RegisterUser(UserCredentials credentials)
    {
        var response = await _fixture.GatewayClient.PostAsJsonAsync(RegisterUrl, credentials.Request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<RegisterResponse>())!.UserId;
    }

    private async Task<string> RegisterAndLogin(UserCredentials credentials)
    {
        await RegisterUser(credentials);
        var loginResponse = await _fixture.GatewayClient.PostAsJsonAsync(
            LoginUrl, new LoginRequest(credentials.Username, credentials.Password));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await loginResponse.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;
    }

    private static UserCredentials NewCredentials()
    {
        var unique = Guid.NewGuid().ToString("N");
        var username = "user" + unique;
        var email = $"{unique}@example.com";
        return new UserCredentials(
            username,
            email,
            "TestPass123",
            new RegisterRequest(username, email, "TestPass123", "Tier B User"));
    }

    private sealed record UserCredentials(string Username, string Email, string Password, RegisterRequest Request);

    private sealed record IdResponse(long Id);
}
