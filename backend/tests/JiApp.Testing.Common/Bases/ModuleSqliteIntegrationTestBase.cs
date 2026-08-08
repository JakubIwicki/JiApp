using JiApp.Common.Services;
using JiApp.Testing.Common.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace JiApp.Testing.Common.Bases;

/// <summary>
/// Module-level integration host on the shared in-memory SQLite store. Adds the
/// per-module seam <see cref="ConfigureModuleServices"/> (called after the store
/// swap) plus the test-identity and service-doubling helpers the Tier A pipeline
/// suites need. Per-module factories override <see cref="ConfigureModuleServices"/>
/// to stub the module's true externals — the remote security-stamp recheck, the
/// user-existence probe, the background workers.
/// </summary>
public abstract class ModuleSqliteIntegrationTestBase<TEntryPoint, TDbContext>
    : SqliteIntegrationTestBase<TEntryPoint, TDbContext>
    where TEntryPoint : class
    where TDbContext : DbContext
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        ConfigureModuleServices(services);
    }

    protected virtual void ConfigureModuleServices(IServiceCollection services)
    {
    }

    /// <summary>
    /// Creates an HttpClient against this factory pre-authorized as
    /// <paramref name="userId"/> with the given permission claims.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(long userId, params string[] permissions)
        => TestTokens.CreateAuthenticatedClient(this, userId, permissions);

    /// <summary>
    /// Replaces the remote security-stamp recheck (registered when IdentityBaseUrl
    /// is configured in the Test appsettings) with the always-valid NoOp validator
    /// so stamp-protected endpoints pass their filter.
    /// </summary>
    protected static void UseNoOpSecurityStampValidator(IServiceCollection services)
    {
        services.RemoveAll<ISecurityStampValidator>();
        services.AddSingleton<ISecurityStampValidator, NoOpSecurityStampValidator>();
    }

    /// <summary>
    /// Removes every hosted service — background workers that would spawn external
    /// processes (yt-dlp) or delete temp files must never run in a pipeline suite.
    /// </summary>
    protected static void RemoveAllHostedServices(IServiceCollection services)
        => services.RemoveAll<IHostedService>();

    /// <summary>
    /// Replaces <typeparamref name="TInterface"/> with the given fake implementation.
    /// </summary>
    protected static void RegisterFake<TInterface, TFake>(IServiceCollection services)
        where TInterface : class
        where TFake : class, TInterface
    {
        services.RemoveAll<TInterface>();
        services.AddSingleton<TInterface, TFake>();
    }
}
