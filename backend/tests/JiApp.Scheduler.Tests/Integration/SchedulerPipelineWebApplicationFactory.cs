using JiApp.Scheduler.Persistence;
using JiApp.Testing.Common.Bases;
using Microsoft.Extensions.DependencyInjection;

namespace JiApp.Scheduler.Tests.Integration;

/// <summary>
/// Full-pipeline host for the Scheduler module on the shared in-memory SQLite
/// store. The Test appsettings configure IdentityBaseUrl, so the Startup registers
/// the real remote security-stamp recheck; this factory replaces it with the
/// always-valid NoOp validator so the stamp-protected endpoints pass their filter.
/// </summary>
public sealed class SchedulerPipelineWebApplicationFactory
    : ModuleSqliteIntegrationTestBase<JiApp.Scheduler.Program, SchedulerDbContext>
{
    protected override void ConfigureModuleServices(IServiceCollection services)
        => UseNoOpSecurityStampValidator(services);
}
