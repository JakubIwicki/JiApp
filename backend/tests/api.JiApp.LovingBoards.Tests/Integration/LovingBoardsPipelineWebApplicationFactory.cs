using api.JiApp.LovingBoards.Clients;
using api.JiApp.LovingBoards.Persistence;
using JiApp.Testing.Common.Bases;
using Microsoft.Extensions.DependencyInjection;

namespace api.JiApp.LovingBoards.Tests.Integration;

/// <summary>
/// Full-pipeline host for the LovingBoards module on the shared in-memory SQLite
/// store. The Test appsettings configure IdentityBaseUrl, so the Startup registers
/// the real remote security-stamp recheck and the real user-existence probe HTTP
/// client; this factory replaces both with their always-success NoOp doubles so no
/// outbound call leaves the test process.
/// </summary>
public sealed class LovingBoardsPipelineWebApplicationFactory
    : ModuleSqliteIntegrationTestBase<api.JiApp.LovingBoards.Program, LovingBoardsDbContext>
{
    protected override void ConfigureModuleServices(IServiceCollection services)
    {
        UseNoOpSecurityStampValidator(services);
        RegisterFake<IUserExistenceClient, NoOpUserExistenceClient>(services);
    }
}
