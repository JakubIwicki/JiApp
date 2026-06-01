using JiApp.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JiApp.YtDownloader.Persistence.Configurations;

public sealed class YoutubeDownloadHistoryConfiguration : IEntityTypeConfiguration<YoutubeDownloadHistory>
{
    public void Configure(EntityTypeBuilder<YoutubeDownloadHistory> builder)
    {
        builder.ToTable("YoutubeDownloadHistory");

        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.HasIndex(e => e.UserId);

        builder.Property(h => h.IsArchived).HasDefaultValue(false);
    }
}