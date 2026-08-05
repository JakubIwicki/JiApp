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
/// Dedicated host for the rate-limit suite with small, testable budgets.
/// Login/Register partition per-IP and every TestServer request shares one
/// remote address, so each fact must build its own host (fresh limiter, fresh
/// database) rather than share a class-level factory whose budgets would leak
/// across facts. The connection is per-instance for the same reason.
/// </summary>
public sealed class IdentityRateLimitWebApplicationFactory : WebApplicationFactory<JiApp.Identity.Program>
{
    private readonly SqliteConnection _connection = CreateConnection();

    static IdentityRateLimitWebApplicationFactory()
    {
        // Same WSL inotify-limit workaround as the main factory.
        Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<IdentityDbContext>>();
            services.RemoveAll<IdentityDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<IdentityDbContext>>();
            services.AddDbContext<IdentityDbContext>(options => options.UseSqlite(_connection));

            RateLimitPolicyOverrides.ApplyBudget(services, loginPermitLimit: 3, registerPermitLimit: 2);
        });
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
