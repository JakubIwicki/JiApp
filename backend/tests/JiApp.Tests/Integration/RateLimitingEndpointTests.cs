using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using JiApp.Common.Abstractions;
using JiApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace JiApp.Tests.Integration;

public class RateLimitingEndpointTests : IDisposable
{
    private static readonly SqliteConnection SharedConnection;

    static RateLimitingEndpointTests()
    {
        SharedConnection = new SqliteConnection("Data Source=RateLimitTests;Mode=Memory;Cache=Shared");
        SharedConnection.Open();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private static WebApplicationFactory<Program> CreateFactoryWithLowLimit(string policySection, int permitLimit)
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting(
                    $"RateLimiting:{policySection}:PermitLimit",
                    permitLimit.ToString());
                builder.UseSetting(
                    $"RateLimiting:{policySection}:WindowInSeconds",
                    "60");
                builder.UseSetting(
                    $"RateLimiting:{policySection}:QueueLimit",
                    "0");

                builder.ConfigureServices(services =>
                {
                    TestDbContextHelper.ReplaceDbContext(services, SharedConnection);
                });
            });

        // Ensure database schema is created for this factory
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JiAppDbContext>();
        db.Database.EnsureCreated();

        return factory;
    }

    // ─── Health endpoint ──────────────────────────────────────

    [Fact]
    public async Task Health_UnderLimit_Returns200()
    {
        var factory = CreateFactoryWithLowLimit("Health", 5);
        var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            var response = await client.GetAsync("/api/health");
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"request {i + 1} should succeed");
        }
    }

    [Fact]
    public async Task Health_OverLimit_Returns429()
    {
        var factory = CreateFactoryWithLowLimit("Health", 2);
        var client = factory.CreateClient();

        await client.GetAsync("/api/health");
        await client.GetAsync("/api/health");

        var response = await client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body.Should().NotBeNull();
        body!.Error.Should().Be("Too many requests. Please try again later.");
        body.RetryAfterSeconds.Should().NotBeNull();
    }

    // ─── Login endpoint ───────────────────────────────────────

    [Fact]
    public async Task Login_UnderLimit_Returns401()
    {
        var factory = CreateFactoryWithLowLimit("Login", 3);
        var client = factory.CreateClient();

        var payload = new { username = "doesnotexist", password = "wrong" };

        for (int i = 0; i < 3; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", payload);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, $"request {i + 1} was under limit and should return 401 for bad credentials");
        }
    }

    [Fact]
    public async Task Login_OverLimit_Returns429()
    {
        var factory = CreateFactoryWithLowLimit("Login", 1);
        var client = factory.CreateClient();

        var payload = new { username = "doesnotexist", password = "wrong" };

        await client.PostAsJsonAsync("/api/auth/login", payload);

        var response = await client.PostAsJsonAsync("/api/auth/login", payload);
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    // ─── Register endpoint ────────────────────────────────────

    [Fact]
    public async Task Register_UnderLimit_ReturnsCreated()
    {
        var factory = CreateFactoryWithLowLimit("Register", 2);
        var client = factory.CreateClient();

        var uniqueSuffix = Guid.NewGuid().ToString("N");
        var payload = new
        {
            username = $"ratelimit_{uniqueSuffix}",
            email = $"ratelimit_{uniqueSuffix}@example.com",
            password = "pass1234",
            displayName = "Rate Limit User"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_OverLimit_Returns429()
    {
        var factory = CreateFactoryWithLowLimit("Register", 1);
        var client = factory.CreateClient();

        var uniqueSuffix = Guid.NewGuid().ToString("N");
        var payload = new
        {
            username = $"ratelimit2_{uniqueSuffix}",
            email = $"ratelimit2_{uniqueSuffix}@example.com",
            password = "pass1234",
            displayName = "Rate Limit User 2"
        };

        await client.PostAsJsonAsync("/api/auth/register", payload);

        var response = await client.PostAsJsonAsync("/api/auth/register", payload);
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    // ─── DownloadFile endpoint ────────────────────────────────

    [Fact]
    public async Task DownloadFile_UnderLimit_Returns404()
    {
        var factory = CreateFactoryWithLowLimit("DownloadFile", 3);
        var client = factory.CreateClient();

        for (int i = 0; i < 3; i++)
        {
            var response = await client.GetAsync($"/api/downloads/mp3/file/nonexistent_{i}");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound, $"request {i + 1} was under limit and should return 404 for non-existent file");
        }
    }

    [Fact]
    public async Task DownloadFile_OverLimit_Returns429()
    {
        var factory = CreateFactoryWithLowLimit("DownloadFile", 1);
        var client = factory.CreateClient();

        await client.GetAsync("/api/downloads/mp3/file/any");

        var response = await client.GetAsync("/api/downloads/mp3/file/any");
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

}
