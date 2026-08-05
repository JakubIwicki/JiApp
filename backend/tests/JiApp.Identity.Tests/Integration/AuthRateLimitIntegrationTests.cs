using System.Net;
using System.Net.Http.Json;
using JiApp.Identity.Features.Auth.Login;
using JiApp.Identity.Features.Auth.Register;

namespace JiApp.Identity.Tests.Integration;

/// <summary>
/// Rate-limit coverage for the Login/Register budgets (G9.6). Login/Register
/// partition per-IP and every TestServer request shares one remote address, so
/// each fact builds its own host — a shared factory would carry its consumed
/// budget into the next fact and make the assertions ordering-dependent.
/// </summary>
public sealed class AuthRateLimitIntegrationTests
{
    private const string LoginUrl = "/api/v1/auth/login";
    private const string RegisterUrl = "/api/v1/auth/register";
    private const string RefreshUrl = "/api/v1/auth/refresh";
    private const string LogoutUrl = "/api/v1/auth/logout";
    private const string ValidPassword = "Password1!";
    private const string UnknownUsername = "ghost";

    [Fact]
    public async Task Facts_ReturnsTooManyRequests_AfterLoginBudgetExhausted()
    {
        using var factory = new IdentityRateLimitWebApplicationFactory();
        var client = factory.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            var login = await client.PostAsJsonAsync(LoginUrl, new LoginRequest(UnknownUsername, ValidPassword));
            login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var fourth = await client.PostAsJsonAsync(LoginUrl, new LoginRequest(UnknownUsername, ValidPassword));

        fourth.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Facts_RegisterSucceeds_WhenLoginBudgetExhausted()
    {
        using var factory = new IdentityRateLimitWebApplicationFactory();
        var client = factory.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            var login = await client.PostAsJsonAsync(LoginUrl, new LoginRequest(UnknownUsername, ValidPassword));
            login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var rejectedLogin = await client.PostAsJsonAsync(LoginUrl, new LoginRequest(UnknownUsername, ValidPassword));
        rejectedLogin.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        var register = await client.PostAsJsonAsync(RegisterUrl,
            new RegisterRequest("independentbudget", "independentbudget@test.com", ValidPassword, "Independent Budget"));

        register.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Facts_ReturnsTooManyRequests_AfterRegisterBudgetExhausted()
    {
        using var factory = new IdentityRateLimitWebApplicationFactory();
        var client = factory.CreateClient();

        for (var i = 0; i < 2; i++)
        {
            var register = await client.PostAsJsonAsync(RegisterUrl,
                new RegisterRequest($"reglimit{i}", $"reglimit{i}@test.com", ValidPassword, $"Register {i}"));
            register.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var third = await client.PostAsJsonAsync(RegisterUrl,
            new RegisterRequest("reglimit2", "reglimit2@test.com", ValidPassword, "Register 2"));

        third.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Facts_RefreshAndLogoutEndpoints_HaveRegisteredPolicies()
    {
        using var factory = new IdentityRateLimitWebApplicationFactory();
        var client = factory.CreateClient();

        // A missing policy makes the rate-limiting middleware throw -> 500. A 400
        // (validation) proves the policy exists and the limiter passed the request
        // through — the load-bearing guard that RefreshPolicy/LogoutPolicy stay
        // registered alongside the exercised Login/Register budgets.
        var refresh = await client.PostAsJsonAsync(RefreshUrl, new { });
        refresh.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var logout = await client.PostAsJsonAsync(LogoutUrl, new { });
        logout.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
