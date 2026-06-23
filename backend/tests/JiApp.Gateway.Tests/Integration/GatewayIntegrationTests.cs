using System.Net;
using System.Text.Json;
using JiApp.Gateway.Tests.Integration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace JiApp.Gateway.Tests.Integration;

public sealed class GatewayIntegrationTests : IClassFixture<GatewayWebApplicationFactory>
{
    private readonly GatewayWebApplicationFactory _factory;

    public GatewayIntegrationTests(GatewayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsJsonBody()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrWhiteSpace();

        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("status").GetString().Should().Be("healthy");
        json.RootElement.GetProperty("timestamp").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task HealthLiveEndpoint_ReturnsJsonBody()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrWhiteSpace();

        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("status").GetString().Should().Be("alive");
    }

    [Fact]
    public async Task HealthReadyEndpoint_ReturnsJsonBody()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrWhiteSpace();

        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("status").GetString().Should().Be("ready");
    }

    [Fact]
    public async Task UnknownEndpoint_Returns404()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/nonexistent");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task HealthDashboard_ReturnsHtml_WithoutExceptionMessages()
    {
        using var devFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
            });
        var client = devFactory.CreateClient();
        var response = await client.GetAsync("/health/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("JiApp Health Dashboard");
        body.Should().Contain("<tr>");
        body.Should().NotContain("UNREACHABLE (");
    }

    [Fact]
    public async Task YarpRoutes_ReturnJsonBody_WhenDownstreamAvailable()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/auth/health");

        var body = await response.Content.ReadAsStringAsync();
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }
}
