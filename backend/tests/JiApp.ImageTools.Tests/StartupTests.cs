#pragma warning disable ASPDEPR004, ASPDEPR008 // WebHostBuilder/GetTestClient obsolete in production but required for WSL test infra
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace JiApp.ImageTools.Tests;

public sealed class StartupTests
{
    private sealed class Fixture
    {
        public HttpClient Client { get; }

        public Fixture()
        {
            var host = new WebHostBuilder()
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    var settings = new ImageToolsSettings();
                    var startup = new Startup(settings);
                    startup.ConfigureServices(services);
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        var tools = endpoints.MapGroup("/api/v1/imagetools");
                        tools.MapGet("/health", () =>
                            Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }));
                        tools.MapGet("/ping", () =>
                            Results.Ok(new { module = "image-tools", status = "ok" }));
                    });
                })
                .Start();
            Client = host.GetTestClient();
        }

        public static Fixture Init() => new();
    }

    [Fact]
    public async Task HealthEndpoint_Returns200()
    {
        var fixture = Fixture.Init();
        using var client = fixture.Client;

        var response = await client.GetAsync("/api/v1/imagetools/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body.Should().NotBeNull();
        body!.status.Should().Be("healthy");
    }

    [Fact]
    public async Task PingEndpoint_Returns200()
    {
        var fixture = Fixture.Init();
        using var client = fixture.Client;

        var response = await client.GetAsync("/api/v1/imagetools/ping");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PingResponse>();
        body.Should().NotBeNull();
        body!.module.Should().Be("image-tools");
        body!.status.Should().Be("ok");
    }

    private sealed record HealthResponse(string status, DateTimeOffset timestamp);

    private sealed record PingResponse(string module, string status);
}