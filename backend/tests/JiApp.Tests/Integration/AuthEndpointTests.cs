using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using JiApp.Common.Abstractions;
using Xunit;

namespace JiApp.Tests.Integration;

public class AuthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_Returns200()
    {
        var response = await _client.GetAsync("/api/v1/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        var payload = new
        {
            username = $"testuser_{Guid.NewGuid():N}",
            email = $"test_{Guid.NewGuid():N}@example.com",
            password = "Pass1234",
            displayName = "Test User"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ReturnsBadRequest()
    {
        var username = $"dupuser_{Guid.NewGuid():N}";
        var payload = new
        {
            username,
            email = $"dup_{Guid.NewGuid():N}@example.com",
            password = "Pass1234",
            displayName = "Test User"
        };

        // First registration should succeed
        var firstResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Second registration with same username should fail with 400
        var duplicatePayload = new
        {
            username,
            email = $"different_{Guid.NewGuid():N}@example.com",
            password = "Pass1234",
            displayName = "Other User"
        };
        var secondResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", duplicatePayload);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await secondResponse.Content.ReadFromJsonAsync<RegisterErrorResponse>();
        body.Should().NotBeNull();
        body.error.Should().Contain("Username");
    }

    [Fact]
    public async Task Register_WithInvalidData_ReturnsBadRequest()
    {
        var payload = new
        {
            username = "",
            email = "not-an-email",
            password = "ab",
            displayName = ""
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.TryGetProperty("errors", out var errorsProperty).Should().BeTrue();
        errorsProperty.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequest()
    {
        var payload = new
        {
            username = $"shortpwd_{Guid.NewGuid():N}",
            email = $"shortpwd_{Guid.NewGuid():N}@example.com",
            password = "short",
            displayName = "Test Short Password"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithPasswordNoUppercase_ReturnsBadRequest()
    {
        var payload = new
        {
            username = $"noupwd_{Guid.NewGuid():N}",
            email = $"noupwd_{Guid.NewGuid():N}@example.com",
            password = "password",
            displayName = "Test No Uppercase"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithPasswordNoDigit_ReturnsBadRequest()
    {
        var payload = new
        {
            username = $"nodigit_{Guid.NewGuid():N}",
            email = $"nodigit_{Guid.NewGuid():N}@example.com",
            password = "Password",
            displayName = "Test No Digit"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DownloadFile_WithInvalidId_ReturnsNotFoundWithApiErrorResponse()
    {
        var (authenticatedClient, _) = await CreateAuthenticatedClientAsync();

        var response = await authenticatedClient.GetAsync("/api/v1/downloads/mp3/file/invalid-id");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body.Should().NotBeNull();
        body.Error.Should().Be("File expired or not found");
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkAndToken()
    {
        // Register a user first
        var username = $"loginuser_{Guid.NewGuid():N}";
        var registerPayload = new
        {
            username,
            email = $"login_{Guid.NewGuid():N}@example.com",
            password = "Pass1234",
            displayName = "Login User"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerPayload);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Login with correct credentials
        var loginPayload = new { username, password = "Pass1234" };
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginPayload);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await loginResponse.Content.ReadFromJsonAsync<LoginSuccessResponse>();
        body.Should().NotBeNull();
        body.Id.Should().BeGreaterThan(0);
        body.DisplayName.Should().Be("Login User");
        body.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var loginPayload = new { username = "nonexistent", password = "wrongpassword" };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginPayload);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body.Should().NotBeNull();
        body.Error.Should().Be("Invalid credentials");
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsOk()
    {
        // Register + Login to get a valid token
        var (authenticatedClient, _) = await CreateAuthenticatedClientAsync();

        var response = await authenticatedClient.GetAsync("/api/v1/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<MeResponseBody>();
        body.Should().NotBeNull();
        body.Id.Should().BeGreaterThan(0);
        body.DisplayName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body.Should().NotBeNull();
        body.Error.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_ReturnsOk()
    {
        // Login to get a valid token
        var (authenticatedClient, _) = await CreateAuthenticatedClientAsync();

        // GET /api/v1/search/history (RequireAuthorization)
        var response = await authenticatedClient.GetAsync("/api/v1/search/history");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/search/history");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body.Should().NotBeNull();
        body.Error.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task DownloadFile_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/downloads/mp3/file/any-id");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Registers a new user and logs in, returning an HttpClient with Bearer token set.
    /// </summary>
    private async Task<(HttpClient Client, string Token)> CreateAuthenticatedClientAsync()
    {
        var username = $"authuser_{Guid.NewGuid():N}";
        var registerPayload = new
        {
            username,
            email = $"auth_{Guid.NewGuid():N}@example.com",
            password = "Pass1234",
            displayName = "Auth User"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerPayload);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginPayload = new { username, password = "Pass1234" };
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginPayload);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginSuccessResponse>();
        loginBody.Should().NotBeNull();

        var authenticatedClient = _factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginBody.Token);
        return (authenticatedClient, loginBody.Token);
    }

    // Response DTOs used for deserializing JSON responses

    private sealed record LoginSuccessResponse(long Id, string? DisplayName, string Token);

    private sealed record MeResponseBody(long Id, string? DisplayName);

    private sealed record RegisterErrorResponse(string error);
}