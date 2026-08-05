using JiApp.Identity.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JiApp.Identity.Tests.Integration;

/// <summary>
/// Full-pipeline host for the Identity API. Shares ONE long-lived in-memory
/// SQLite connection per factory instance so the host-startup migration
/// (Program.Migrate() + RoleSeeder) and every request scope see the same
/// database — the connection OBJECT is registered, never a connection string
/// (EF opening its own ":memory:" per request would yield a fresh empty DB).
/// The connection is instance-scoped: a static one would be torn down by the
/// first consumer's Dispose and break any later factory.
/// </summary>
public sealed class IdentityWebApplicationFactory : WebApplicationFactory<JiApp.Identity.Program>
{
    private readonly SqliteConnection _connection = CreateConnection();

    static IdentityWebApplicationFactory()
    {
        // WSL hits the 128-inotify-instance limit when the test runner restarts
        // the file watcher; polling avoids the host build crashing under it.
        Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureServices(services =>
        {
            // Swap the store registration for the shared in-memory connection —
            // remove the production options/context/configuration first so the
            // factory's AddDbContext below is the only one that applies.
            services.RemoveAll<DbContextOptions<IdentityDbContext>>();
            services.RemoveAll<IdentityDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<IdentityDbContext>>();
            services.AddDbContext<IdentityDbContext>(options => options.UseSqlite(_connection));

            // Raise the auth budgets far above what the normal-flow facts use; the
            // dedicated rate-limit suite (IdentityRateLimitWebApplicationFactory)
            // applies small values on its own host.
            RateLimitPolicyOverrides.ApplyBudget(services, loginPermitLimit: 100, registerPermitLimit: 100);
        });
    }

    /// <summary>
    /// Reads persisted state through a fresh request scope on the shared
    /// connection — the integration-test counterpart of "assert the store".
    /// </summary>
    public T InFreshScope<T>(Func<IdentityDbContext, T> read)
    {
        using var scope = Services.CreateScope();
        return read(scope.ServiceProvider.GetRequiredService<IdentityDbContext>());
    }

    private static SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
