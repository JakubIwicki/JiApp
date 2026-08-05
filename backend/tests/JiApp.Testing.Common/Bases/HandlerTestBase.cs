using JiApp.Testing.Common.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Testing.Common.Bases;

public abstract class HandlerTestBase<TDbContext> : IDisposable
    where TDbContext : DbContext
{
    private readonly SqliteConnection _connection;
    private readonly TDbContext _dbContext;

    protected TestDb Db { get; }
    protected TDbContext DbContext => _dbContext;

    protected HandlerTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using var pragmaCmd = _connection.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
        pragmaCmd.ExecuteNonQuery();
        var options = new DbContextOptionsBuilder<TDbContext>()
            .UseSqlite(_connection)
            .Options;
        _dbContext = (TDbContext)Activator.CreateInstance(typeof(TDbContext), options)!;
        _dbContext.Database.EnsureCreated();
        Db = new TestDb(_dbContext);
    }

    protected void StoreInDb<T>(T entity) where T : class => Db.Store(entity);
    protected void RemoveFromDb<T>(T entity) where T : class => Db.Remove(entity);

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
