using Microsoft.Data.Sqlite;

namespace api.JiApp.LovingBoards.Tests.Bases;

public abstract class LovingBoardsHandlerTestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LovingBoardsDbContext _dbContext;

    protected TestDb Db { get; }
    protected ILovingBoardsDbContext DbContext => _dbContext;

    protected LovingBoardsHandlerTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using var pragmaCmd = _connection.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
        pragmaCmd.ExecuteNonQuery();
        var options = new DbContextOptionsBuilder<LovingBoardsDbContext>()
            .UseSqlite(_connection)
            .Options;
        _dbContext = new LovingBoardsDbContext(options);
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
