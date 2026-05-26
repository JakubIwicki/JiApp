using JiApp.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JiApp.Infrastructure.Persistence.Configurations;

public class YoutubeSearchHistoryConfiguration : IEntityTypeConfiguration<YoutubeSearchHistory>
{
    public void Configure(EntityTypeBuilder<YoutubeSearchHistory> builder)
    {
        builder.ToTable("YoutubeSearchHistory");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.UserId);

        builder.Property(h => h.IsArchived).HasDefaultValue(false);
    }
}