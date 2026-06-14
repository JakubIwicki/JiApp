using JiApp.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JiApp.Identity.Persistence.Configurations;

public sealed class UserModuleGrantConfiguration : IEntityTypeConfiguration<UserModuleGrant>
{
    public void Configure(EntityTypeBuilder<UserModuleGrant> builder)
    {
        builder.ToTable("UserModuleGrants");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ModuleName)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.GrantedAt);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.UserId, e.ModuleName })
            .IsUnique();
    }
}
