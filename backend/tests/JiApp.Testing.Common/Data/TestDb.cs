using Microsoft.EntityFrameworkCore;

namespace JiApp.Testing.Common.Data;

public sealed class TestDb
{
    private readonly DbContext _db;

    public TestDb(DbContext db) => _db = db;

    public void Store<T>(T entity) where T : class
    {
        _db.Set<T>().Add(entity);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
    }

    public void StoreAll<T>(params T[] entities) where T : class
    {
        _db.Set<T>().AddRange(entities);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
    }

    public void Remove<T>(T entity) where T : class
    {
        _db.Set<T>().Remove(entity);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
    }

    public T? Find<T>(object id) where T : class =>
        _db.Set<T>().Find(id);

    public T? FindFresh<T>(object id) where T : class
    {
        _db.ChangeTracker.Clear();
        return _db.Set<T>().Find(id);
    }

    public IQueryable<T> Query<T>() where T : class =>
        _db.Set<T>();
}
