using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JiApp.Testing.Common.Bases;

/// <summary>
/// Shared WebApplicationFactory base for integration suites whose host uses a
/// SQLite-backed DbContext. Swaps the production store registration for ONE
/// long-lived in-memory SQLite connection per factory instance so the
/// host-startup migration (Program.Migrate() + seeder) and every request scope
/// see the same database — the connection OBJECT is registered, never a
/// connection string (EF opening its own ":memory:" per request would yield a
/// fresh empty DB). The connection is instance-scoped: a static one would be
/// torn down by the first consumer's Dispose and break any later factory.
/// A derived factory must override <see cref="ConfigureServices"/> and call
/// base.ConfigureServices(services) before applying its own service additions.
/// </summary>
public abstract class SqliteIntegrationTestBase<TEntryPoint, TDbContext>
    : IntegrationTestBase<TEntryPoint>
    where TEntryPoint : class
    where TDbContext : DbContext
{
    private readonly SqliteConnection _connection = CreateConnection();

    protected override void ConfigureServices(IServiceCollection services)
    {
        // Swap the store registration for the shared in-memory connection —
        // remove the production options/context/configuration first so the
        // AddDbContext below is the only one that applies.
        services.RemoveAll<DbContextOptions<TDbContext>>();
        services.RemoveAll<TDbContext>();
        services.RemoveAll<IDbContextOptionsConfiguration<TDbContext>>();
        services.AddDbContext<TDbContext>(options => options.UseSqlite(_connection));
    }

    /// <summary>
    /// Reads persisted state through a fresh request scope on the shared
    /// connection — the integration-test counterpart of "assert the store".
    /// </summary>
    public T InFreshScope<T>(Func<TDbContext, T> read)
    {
        using var scope = Services.CreateScope();
        return read(scope.ServiceProvider.GetRequiredService<TDbContext>());
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
