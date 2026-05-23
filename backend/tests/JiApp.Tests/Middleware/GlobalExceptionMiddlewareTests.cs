using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using JiApp.Common.Abstractions;
using JiApp.Tests.Integration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace JiApp.Tests.Middleware;

public class GlobalExceptionMiddlewareTests(ConfigOnlyWebApplicationFactory factory)
    : IClassFixture<ConfigOnlyWebApplicationFactory>
{
    private readonly WebApplicationFactory<Program> _factory = factory;

    [Fact]
    public async Task NonExistentEndpoint_Returns404_InProduction()
    {
        var productionFactory = _factory.WithWebHostBuilder(builder => { builder.UseEnvironment("Production"); });
        var client = productionFactory.CreateClient();

        var response = await client.GetAsync("/api/v1/nonexistent");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnhandledException_Returns500WithStructuredError_WhenInDevelopment()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/throw");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        body.Should().NotBeNull();
        body.Error.Should().Be("test error");
    }

    [Fact]
    public async Task HealthEndpoint_Works_WithMiddlewareInPlace()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthDto>();
        body!.Status.Should().Be("healthy");
    }

    private sealed record HealthDto(string Status, DateTime Timestamp);
}