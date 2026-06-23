using JiApp.Scheduler.Persistence;
using JiApp.Testing.Common.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Testing.Common.Bases;

public abstract class HandlerTestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SchedulerDbContext _dbContext;

    protected TestDb Db { get; }
    protected ISchedulerDbContext DbContext => _dbContext;

    protected HandlerTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseSqlite(_connection)
            .Options;
        _dbContext = new SchedulerDbContext(options);
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
