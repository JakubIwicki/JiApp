namespace JiApp.Gateway.Tests.Integration;

/// <summary>
/// Guards the Wave 6 (G9.6) factory consolidation: the Gateway integration
/// factory must derive from the shared base so environment + WSL watcher
/// handling stay in one place.
/// </summary>
public sealed class FactoryHierarchyConventionTests
{
    [Fact]
    public void GatewayFactory_DerivesFromIntegrationTestBase()
    {
        typeof(GatewayWebApplicationFactory).IsAssignableTo(
            typeof(IntegrationTestBase<JiApp.Gateway.Program>)).Should().BeTrue();
    }
}
