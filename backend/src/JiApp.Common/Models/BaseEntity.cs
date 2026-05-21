// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace JiApp.Common.Models;

public abstract class BaseEntity<TKey>
    where TKey : IEquatable<TKey>, IComparable<TKey>
{
    public TKey Id { get; set; } = default!;
}
