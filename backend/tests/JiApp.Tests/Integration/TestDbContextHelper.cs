using JiApp.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JiApp.Tests.Integration;

/// <summary>
/// Shared helper for replacing the EF Core SQLite DbContext registration
/// with a test-specific in-memory SQLite connection.
/// </summary>
internal static class TestDbContextHelper
{
    /// <summary>
    /// Removes the existing <see cref="JiAppDbContext"/> and <see cref="DbContextOptions{JiAppDbContext}"/>
    /// registrations from the service collection and replaces them with a new registration
    /// using the provided <paramref name="connection"/>.
    /// </summary>
    public static void ReplaceDbContext(IServiceCollection services, SqliteConnection connection)
    {
        var dbOptionsDescriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(DbContextOptions<JiAppDbContext>));
        if (dbOptionsDescriptor is not null)
            services.Remove(dbOptionsDescriptor);

        var dbContextDescriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(JiAppDbContext));
        if (dbContextDescriptor is not null)
            services.Remove(dbContextDescriptor);

        services.AddDbContext<JiAppDbContext>(options =>
            options.UseSqlite(connection));
    }
}
