using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Testing.Common.Data;

/// <summary>
/// An in-memory SQLite context wired with a <see cref="QueryCountInterceptor"/>
/// so a test can drive the real EF query pipeline and assert round-trip counts.
/// </summary>
public sealed class SqliteQueryCountFixture<TDbContext> : IDisposable
    where TDbContext : DbContext
{
    private readonly SqliteConnection _connection;

    public TDbContext Db { get; }
    public QueryCountInterceptor Interceptor { get; }

    private SqliteQueryCountFixture(SqliteConnection connection, TDbContext db, QueryCountInterceptor interceptor)
    {
        _connection = connection;
        Db = db;
        Interceptor = interceptor;
    }

    public static SqliteQueryCountFixture<TDbContext> Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var pragmaCommand = connection.CreateCommand();
        pragmaCommand.CommandText = "PRAGMA foreign_keys = ON;";
        pragmaCommand.ExecuteNonQuery();
        var interceptor = new QueryCountInterceptor();
        var options = new DbContextOptionsBuilder<TDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(interceptor)
            .Options;
        var db = (TDbContext)Activator.CreateInstance(typeof(TDbContext), options)!;
        db.Database.EnsureCreated();
        return new SqliteQueryCountFixture<TDbContext>(connection, db, interceptor);
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
