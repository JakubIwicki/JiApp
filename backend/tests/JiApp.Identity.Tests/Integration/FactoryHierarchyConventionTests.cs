using JiApp.Identity.Persistence;

namespace JiApp.Identity.Tests.Integration;

/// <summary>
/// Guards the Wave 6 (G9.6) factory consolidation: Identity integration
/// factories must derive from the shared SQLite base so store-swap and
/// connection lifecycle stay in one place.
/// </summary>
public sealed class FactoryHierarchyConventionTests
{
    [Fact]
    public void IdentityFactories_DeriveFromSqliteIntegrationTestBase()
    {
        typeof(IdentityWebApplicationFactory).IsAssignableTo(
            typeof(SqliteIntegrationTestBase<JiApp.Identity.Program, IdentityDbContext>)).Should().BeTrue();
        typeof(IdentityRateLimitWebApplicationFactory).IsAssignableTo(
            typeof(SqliteIntegrationTestBase<JiApp.Identity.Program, IdentityDbContext>)).Should().BeTrue();
    }
}
