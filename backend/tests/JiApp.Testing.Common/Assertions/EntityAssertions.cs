using Microsoft.EntityFrameworkCore;

namespace JiApp.Testing.Common.Assertions;

public static class EntityAssertions
{
    public static void AssertEntityExists<T>(DbContext db, long id) where T : class
    {
        var entity = db.Set<T>().Find(id);
        entity.Should().NotBeNull();
    }

    public static void AssertEntityCount<T>(DbContext db, int expected) where T : class =>
        db.Set<T>().Count().Should().Be(expected);

    public static void AssertNoEntityInDb<T>(DbContext db) where T : class =>
        db.Set<T>().Should().BeEmpty();

    public static void AssertEntityDoesNotExist<T>(DbContext db, long id) where T : class
    {
        var entity = db.Set<T>().Find(id);
        entity.Should().BeNull();
    }

    public static void AssertEntityDoesNotExist<T>(DbContext db, Func<T, bool> predicate) where T : class
    {
        var entity = db.Set<T>().AsEnumerable().FirstOrDefault(predicate);
        entity.Should().BeNull();
    }

    public static void AssertSingleEntityInDb<T>(DbContext db) where T : class =>
        db.Set<T>().Should().ContainSingle();
}
