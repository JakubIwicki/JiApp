using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using JiApp.Identity.Features.Auth.Login;
using JiApp.Identity.Features.Auth.Register;

namespace JiApp.Identity.Tests.Integration;

/// <summary>
/// Full-pipeline suite for <c>GET /api/v1/auth/users/{userId}/exists</c> — the
/// cross-service existence probe consumed by the LovingBoards member-add guard.
/// </summary>
public sealed class UserExistsEndpointTests : IClassFixture<IdentityWebApplicationFactory>
{
    private const string RegisterUrl = "/api/v1/auth/register";
    private const string LoginUrl = "/api/v1/auth/login";
    private const string ExistsUrl = "/api/v1/auth/users/";
    private const string ValidPassword = "Password1!";

    private readonly HttpClient _client;

    public UserExistsEndpointTests(IdentityWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Returns200_WhenUserExists()
    {
        var (userId, token) = await RegisterAndLoginAsync("existsuser200", "existsuser200@test.com");

        using var request = AuthenticatedRequest(HttpMethod.Get, $"{ExistsUrl}{userId}/exists", token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Returns404_WhenUserDoesNotExist()
    {
        var (_, token) = await RegisterAndLoginAsync("existsuser404", "existsuser404@test.com");

        using var request = AuthenticatedRequest(HttpMethod.Get, $"{ExistsUrl}999999/exists", token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Returns401_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync($"{ExistsUrl}1/exists");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<(long UserId, string AccessToken)> RegisterAndLoginAsync(string username, string email)
    {
        var register = await _client.PostAsJsonAsync(RegisterUrl,
            new RegisterRequest(username, email, ValidPassword, "Test User"));
        register.StatusCode.Should().Be(HttpStatusCode.Created);
        var registered = (await register.Content.ReadFromJsonAsync<RegisterResponse>())!;

        var login = await _client.PostAsJsonAsync(LoginUrl, new LoginRequest(username, ValidPassword));
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var loggedIn = (await login.Content.ReadFromJsonAsync<LoginResponse>())!;

        return (registered.UserId, loggedIn.AccessToken);
    }

    private static HttpRequestMessage AuthenticatedRequest(HttpMethod method, string url, string accessToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }
}
