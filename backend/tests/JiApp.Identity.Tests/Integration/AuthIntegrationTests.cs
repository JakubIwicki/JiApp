using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using JiApp.Common;
using JiApp.Common.Abstractions;
using JiApp.Identity.Features.Auth.Login;
using JiApp.Identity.Features.Auth.Register;
using JiApp.Identity.Services;

namespace JiApp.Identity.Tests.Integration;

/// <summary>
/// Full-pipeline suite for the Identity API (G9.6): real HTTP through routing,
/// auth middleware, handlers and a real migrated in-memory store — including the
/// OnTokenValidated security-stamp recheck that handler mock suites cannot reach.
/// </summary>
public sealed class AuthIntegrationTests : IClassFixture<IdentityWebApplicationFactory>
{
    private const string RegisterUrl = "/api/v1/auth/register";
    private const string LoginUrl = "/api/v1/auth/login";
    private const string MeUrl = "/api/v1/auth/me";
    private const string AdminUsersUrl = "/api/v1/auth/admin/users";
    private const string ValidPassword = "Password1!";

    private readonly IdentityWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthIntegrationTests(IdentityWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Facts_RegisterThenLogin_ReturnsAccessAndRefreshTokens()
    {
        const string username = "roundtrip";

        var register = await _client.PostAsJsonAsync(RegisterUrl,
            new RegisterRequest(username, "roundtrip@test.com", ValidPassword, "Round Trip"));

        register.StatusCode.Should().Be(HttpStatusCode.Created);

        var login = await _client.PostAsJsonAsync(LoginUrl, new LoginRequest(username, ValidPassword));

        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Facts_DuplicateRegister_ReturnsBadRequest_AndPersistsSingleUser()
    {
        const string username = "duplicateregister";

        var first = await RegisterAsync(_client, username, "dup1@test.com");
        first.Should().NotBeNull();

        var duplicate = await _client.PostAsJsonAsync(RegisterUrl,
            new RegisterRequest(username, "dup2@test.com", ValidPassword, "Second"));

        duplicate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await duplicate.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error.Should().NotBeNull();
        error!.Error.Should().NotBeNullOrWhiteSpace();
        // Known latent finding (G9.6): the default UserValidator pre-checks
        // username uniqueness via FindByNameAsync and leaks "Username 'x' is
        // already taken." — an enumeration vector the RegisterHandler's generic
        // DB-constraint path never reaches. Assert the current behavior so the
        // eventual fix surfaces as a test change.
        error.Error.Should().Contain("already taken");
        _factory.InFreshScope(db => db.Users.Count(u => u.UserName == username)).Should().Be(1);
    }

    [Fact]
    public async Task Facts_DuplicateEmail_ReturnsBadRequest_AndPersistsSingleUser()
    {
        const string username = "dupemaila";
        const string email = "shared@test.com";

        await RegisterAsync(_client, username, email);

        var duplicate = await _client.PostAsJsonAsync(RegisterUrl,
            new RegisterRequest("dupemailb", email, ValidPassword, "Second"));

        duplicate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await duplicate.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error.Should().NotBeNull();
        error!.Error.Should().NotBeNullOrWhiteSpace();
        // Known latent finding (G9.6): the default UserValidator's
        // RequireUniqueEmail pre-check via FindByEmailAsync leaks "Email 'x' is
        // already taken." — the same prod duplicate-email enumeration vector
        // already recorded in the Wave 5 closeout backlog. Assert the current
        // behavior so the eventual fix surfaces as a change to this assertion.
        error.Error.Should().Contain("already taken");
        _factory.InFreshScope(db => db.Users.Count(u => u.Email == email)).Should().Be(1);
    }

    [Fact]
    public async Task Facts_LoginReturnsJwt_WithRoleAndSecurityStampClaims()
    {
        const string username = "jwtclaims";
        var login = await RegisterAndLoginAsync(_client, username, "jwtclaims@test.com");

        var token = DecodeToken(login.AccessToken);
        var roleClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        roleClaim.Should().NotBeNull();
        roleClaim!.Value.Should().Be(RoleNames.Guest);
        var stampClaim = token.Claims.FirstOrDefault(c => c.Type == JwtTokenService.SecurityStampClaimType);
        stampClaim.Should().NotBeNull();
        stampClaim!.Value.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Facts_ReturnsUnauthorized_WhenPasswordInvalid()
    {
        const string username = "invalidpassword";
        await RegisterAsync(_client, username, "invalidpassword@test.com");

        var login = await _client.PostAsJsonAsync(LoginUrl, new LoginRequest(username, "WrongPassword1!"));

        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var error = await login.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error.Should().NotBeNull();
        error!.Error.Should().Be("Invalid credentials");
    }

    [Fact]
    public async Task Facts_RejectsPreviouslyIssuedToken_AfterSecurityStampRotated()
    {
        const string username = "stamprotate";
        const string rotatedStamp = "rotated-after-issue";
        var login = await RegisterAndLoginAsync(_client, username, "stamprotate@test.com");

        var storedStamp = _factory.InFreshScope(db =>
        {
            var user = db.Users.Single(u => u.UserName == username);
            user.SecurityStamp = rotatedStamp;
            db.SaveChanges();
            return user.SecurityStamp;
        });
        storedStamp.Should().Be(rotatedStamp);

        using var request = AuthenticatedRequest(HttpMethod.Get, MeUrl, login.AccessToken);
        var me = await _client.SendAsync(request);

        me.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var error = await me.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error.Should().NotBeNull();
        error!.Error.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task Facts_ReturnsUnauthorized_WhenAuthorizationHeaderMissing()
    {
        var response = await _client.GetAsync(MeUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error.Should().NotBeNull();
        error!.Error.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task Facts_ReturnsForbidden_WhenNonAdminCallsAdminEndpoint()
    {
        const string username = "plainuser";
        var login = await RegisterAndLoginAsync(_client, username, "plainuser@test.com");

        using var request = AuthenticatedRequest(HttpMethod.Get, AdminUsersUrl, login.AccessToken);
        var admin = await _client.SendAsync(request);

        admin.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static async Task<RegisterResponse> RegisterAsync(HttpClient client, string username, string email)
    {
        var register = await client.PostAsJsonAsync(RegisterUrl,
            new RegisterRequest(username, email, ValidPassword, "Test User"));
        register.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await register.Content.ReadFromJsonAsync<RegisterResponse>())!;
    }

    private static async Task<LoginResponse> RegisterAndLoginAsync(HttpClient client, string username, string email)
    {
        await RegisterAsync(client, username, email);

        var login = await client.PostAsJsonAsync(LoginUrl, new LoginRequest(username, ValidPassword));
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await login.Content.ReadFromJsonAsync<LoginResponse>())!;
    }

    private static HttpRequestMessage AuthenticatedRequest(HttpMethod method, string url, string accessToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private static JwtSecurityToken DecodeToken(string accessToken) =>
        new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
}
