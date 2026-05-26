using JiApp.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JiApp.Infrastructure.Persistence.Configurations;

public class YoutubeDownloadHistoryConfiguration : IEntityTypeConfiguration<YoutubeDownloadHistory>
{
    public void Configure(EntityTypeBuilder<YoutubeDownloadHistory> builder)
    {
        builder.ToTable("YoutubeDownloadHistory");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.UserId);

        builder.Property(h => h.IsArchived).HasDefaultValue(false);
    }
}