using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using JiApp.Common.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JiApp.Tests.Middleware;

public class GlobalExceptionMiddlewareTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GlobalExceptionMiddlewareTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NonExistentEndpoint_Returns404_InProduction()
    {
        var productionFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
        });
        var client = productionFactory.CreateClient();

        var response = await client.GetAsync("/api/nonexistent");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnhandledException_Returns500WithStructuredError_WhenInDevelopment()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/throw");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body.Should().NotBeNull();
        body!.Error.Should().Be("test error");
    }

    [Fact]
    public async Task HealthEndpoint_Works_WithMiddlewareInPlace()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthDto>();
        body!.Status.Should().Be("healthy");
    }

    private sealed record HealthDto(string Status, DateTime Timestamp);
}
