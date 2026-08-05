using JiApp.Identity.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace JiApp.Identity.Tests.Integration;

/// <summary>
/// Full-pipeline host for the Identity API. Derives from
/// <see cref="SqliteIntegrationTestBase{TEntryPoint,TDbContext}"/>, which swaps
/// the store for a shared in-memory SQLite connection (see its doc for why the
/// connection OBJECT is registered). Raises the auth budgets far above what the
/// normal-flow facts use; the dedicated rate-limit suite
/// (IdentityRateLimitWebApplicationFactory) applies small values on its own host.
/// </summary>
public sealed class IdentityWebApplicationFactory
    : SqliteIntegrationTestBase<JiApp.Identity.Program, IdentityDbContext>
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        RateLimitPolicyOverrides.ApplyBudget(services, loginPermitLimit: 100, registerPermitLimit: 100);
    }
}
