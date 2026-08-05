namespace JiApp.Common.Models;

public abstract class BaseEntity<TKey>
    where TKey : IEquatable<TKey>
{
    // protected (not private) so an entity with a client-generated key (e.g. a string
    // temp id) can assign it once in its static factory; the ORM materialises the rest.
    public TKey Id { get; protected set; } = default!;
}