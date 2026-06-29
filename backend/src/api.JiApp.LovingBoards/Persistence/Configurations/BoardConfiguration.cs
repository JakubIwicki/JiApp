using System.Text.Json;
using api.JiApp.LovingBoards.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.JiApp.LovingBoards.Persistence.Configurations;

public sealed class BoardConfiguration : IEntityTypeConfiguration<Board>
{
    private static readonly ValueComparer<List<long>> MemberUserIdsComparer = new(
        (a, b) => (a ?? new List<long>()).SequenceEqual(b ?? new List<long>()),
        v => v.Aggregate(0, (h, x) => HashCode.Combine(h, x.GetHashCode())),
        v => v.ToList());

    public void Configure(EntityTypeBuilder<Board> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.OwnerUserId).IsRequired();
        builder.Property(x => x.MemberUserIds)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<long>>(v, (JsonSerializerOptions?)null) ?? new List<long>())
            .HasColumnType("TEXT")
            .Metadata.SetValueComparer(MemberUserIdsComparer);
    }
}
