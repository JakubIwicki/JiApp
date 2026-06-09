namespace JiApp.Common.Models;

public abstract class BaseEntity<TKey>
    where TKey : IEquatable<TKey>
{
    public TKey Id { get; private set; } = default!;
}