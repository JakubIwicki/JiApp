using System;
using System.Threading.Tasks;
using JiApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

#pragma warning disable CA1515 // Test infrastructure types must be public for xUnit

namespace JiApp.Tests.Integration;

/// <summary>
/// Provides test configuration keys (JWT, YouTube API) that Startup validations require.
/// Use as the base for WebApplicationFactory test fixtures that don't need database setup.
/// </summary>
public static class TestConfiguration
{
    public static readonly string JwtKey = "test-jwt-key-that-is-at-least-32-characters-for-hmac!";
    public static readonly string YoutubeApiKey = "test-youtube-api-key";
}

/// <summary>
/// WebApplicationFactory for tests that need only config overrides (no database setup).
/// Provides JWT signing key and YouTube API key so Startup validations pass.
/// </summary>
public class ConfigOnlyWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseSetting("Jwt:Key", TestConfiguration.JwtKey);
        builder.UseSetting("Youtube:api-key", TestConfiguration.YoutubeApiKey);
    }
}

/// <summary>
/// Custom WebApplicationFactory that replaces the SQLite database with an in-memory
/// shared instance for test isolation. Provides test JWT signing key and YouTube API
/// key so the Startup validations pass.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        _connection = new SqliteConnection("Data Source=JiApp_Test;Mode=Memory;Cache=Shared");
        _connection.Open();

        builder.UseSetting("Jwt:Key", TestConfiguration.JwtKey);
        builder.UseSetting("Youtube:api-key", TestConfiguration.YoutubeApiKey);

        builder.ConfigureServices(services => { TestDbContextHelper.ReplaceDbContext(services, _connection!); });
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

        await ((IAsyncDisposable)this).DisposeAsync();
    }
}