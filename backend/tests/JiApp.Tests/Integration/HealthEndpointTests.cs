using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace JiApp.Tests.Integration;

public class HealthEndpointTests(ConfigOnlyWebApplicationFactory factory)
    : IClassFixture<ConfigOnlyWebApplicationFactory>
{
    [Fact]
    public async Task GetHealth_Returns200WithStatusAndTimestamp()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body.Should().NotBeNull();
        body.Status.Should().Be("healthy");
        body.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    private sealed record HealthResponse(string Status, DateTime Timestamp);
}