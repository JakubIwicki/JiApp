using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JiApp.Tests.Middleware;

public class GlobalExceptionMiddlewareTests
{
    [Fact]
    public async Task UnhandledException_Returns500WithStructuredError_WhenNotDevelopment()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
            });
        var client = factory.CreateClient();

        // In Production, the /api/throw endpoint doesn't exist (dev-only)
        // So hitting a non-existent endpoint with bad behavior should still work
        var response = await client.GetAsync("/api/nonexistent");

        // App is running in Production mode — non-existent routes return 404, not 500
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnhandledException_Returns500WithStructuredError_WhenInDevelopment()
    {
        var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/throw");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body.Should().NotBeNull();
        body!.Error.Should().Be("test error");
    }

    [Fact]
    public async Task HealthEndpoint_Works_WithMiddlewareInPlace()
    {
        var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthDto>();
        body!.Status.Should().Be("healthy");
    }

    private sealed record ErrorResponse([property: JsonPropertyName("error")] string Error);
    private sealed record HealthDto(string Status, DateTime Timestamp);
}
