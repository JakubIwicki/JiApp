using System.Net;

namespace JiApp.Gateway.Tests.Integration.CrossModule;

/// <summary>
/// Boots the four hosts for the cross-module Gateway E2E tier. The three module
/// hosts listen on real ephemeral Kestrel sockets; the Gateway stays on the
/// in-memory TestServer and proxies to those sockets. Each host's health endpoint
/// is probed over the real socket before the suite proceeds — if a host failed to
/// boot, the fixture throws here instead of the scenario tests failing obscurely.
/// </summary>
public sealed class GatewayCrossModuleFixture : IDisposable
{
    public CrossModuleIdentityWebApplicationFactory Identity { get; }
    public CrossModuleSchedulerWebApplicationFactory Scheduler { get; }
    public CrossModuleLovingBoardsWebApplicationFactory LovingBoards { get; }
    public CrossModuleGatewayWebApplicationFactory Gateway { get; }
    public HttpClient GatewayClient { get; }

    public GatewayCrossModuleFixture()
    {
        Identity = new CrossModuleIdentityWebApplicationFactory();
        var identityUrl = Identity.BaseAddress.ToString();
        Get(identityUrl + "api/v1/auth/health").Should().Be(HttpStatusCode.OK);

        Scheduler = new CrossModuleSchedulerWebApplicationFactory(identityUrl);
        Get(Scheduler.BaseAddress.ToString() + "api/v1/scheduler/health").Should().Be(HttpStatusCode.OK);

        LovingBoards = new CrossModuleLovingBoardsWebApplicationFactory(identityUrl);
        Get(LovingBoards.BaseAddress.ToString() + "api/v1/lovingboards/health").Should().Be(HttpStatusCode.OK);

        Gateway = new CrossModuleGatewayWebApplicationFactory(
            identityUrl, Scheduler.BaseAddress.ToString(), LovingBoards.BaseAddress.ToString());
        GatewayClient = Gateway.CreateClient();
    }

    public void Dispose()
    {
        Gateway?.Dispose();
        LovingBoards?.Dispose();
        Scheduler?.Dispose();
        Identity?.Dispose();
    }

    private static HttpStatusCode Get(string url)
    {
        using var client = new HttpClient();
        return client.GetAsync(url).GetAwaiter().GetResult().StatusCode;
    }
}
