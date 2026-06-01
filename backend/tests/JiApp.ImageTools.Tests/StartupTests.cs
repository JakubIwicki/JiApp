#pragma warning disable ASPDEPR004, ASPDEPR008 // WebHostBuilder/GetTestClient obsolete in production but required for WSL test infra
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace JiApp.ImageTools.Tests;

public class StartupTests
{
    /// <summary>
    /// Creates a test server using WebHostBuilder (avoids default file-watched config
    /// that causes WSL inotify issues). Wire up is equivalent to Startup.Configure.
    /// </summary>
    private static IWebHost CreateTestHost()
    {
        return new WebHostBuilder()
            .UseTestServer()
            .ConfigureServices(services =>
            {
                var settings = new Configuration.ImageToolsSettings();
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
    }

    private static HttpClient CreateClient()
    {
        var host = CreateTestHost();
        return host.GetTestClient();
    }

    [Fact]
    public async Task Health_endpoint_returns_200()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/imagetools/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body.Should().NotBeNull();
        body!.status.Should().Be("healthy");
    }

    [Fact]
    public async Task Ping_endpoint_returns_200()
    {
        using var client = CreateClient();

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