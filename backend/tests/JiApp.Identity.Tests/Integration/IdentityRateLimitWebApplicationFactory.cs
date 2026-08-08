using JiApp.Identity.Persistence;
using JiApp.Testing.Common.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace JiApp.Identity.Tests.Integration;

/// <summary>
/// Dedicated host for the rate-limit suite with small, testable budgets.
/// Login/Register partition per-IP and every TestServer request shares one
/// remote address, so each fact must build its own host (fresh limiter, fresh
/// database) rather than share a class-level factory whose budgets would leak
/// across facts. The per-instance shared connection comes from the base.
/// </summary>
public sealed class IdentityRateLimitWebApplicationFactory
    : SqliteIntegrationTestBase<JiApp.Identity.Program, IdentityDbContext>
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        RateLimitPolicyOverrides.ApplyBudget(services, loginPermitLimit: 3, registerPermitLimit: 2);
    }
}
