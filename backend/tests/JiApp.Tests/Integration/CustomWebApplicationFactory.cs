using JiApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace JiApp.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory that replaces the SQLite database with an in-memory
/// shared instance for test isolation. JWT and other configuration is inherited from
/// the development configuration (appsettings.Development.json).
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection = new SqliteConnection("Data Source=JiApp_Test;Mode=Memory;Cache=Shared");
        _connection.Open();

        // Replace the DbContext registration to use our in-memory SQLite database.
        // We do this via ConfigureServices (which runs after Program.cs has registered
        // everything) rather than ConfigureAppConfiguration, because Program.cs reads
        // config values (like Jwt:Key) into local variables before our overrides would
        // take effect. By replacing the DbContext registration directly, we keep JWT
        // settings from appsettings.Development.json consistent between token generation
        // and validation.
        builder.ConfigureServices(services =>
        {
            TestDbContextHelper.ReplaceDbContext(services, _connection!);
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JiAppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        // xUnit only calls IAsyncLifetime.DisposeAsync(), not IAsyncDisposable.DisposeAsync()
        // when both are implemented, so we chain to the base's async disposal explicitly.
        await ((IAsyncDisposable)this).DisposeAsync();
    }
}
