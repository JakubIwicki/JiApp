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
}
