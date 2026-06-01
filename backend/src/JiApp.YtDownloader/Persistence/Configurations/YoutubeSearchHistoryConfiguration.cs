using JiApp.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JiApp.YtDownloader.Persistence.Configurations;

public sealed class YoutubeSearchHistoryConfiguration : IEntityTypeConfiguration<YoutubeSearchHistory>
{
    public void Configure(EntityTypeBuilder<YoutubeSearchHistory> builder)
    {
        builder.ToTable("YoutubeSearchHistory");

        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.HasIndex(e => e.UserId);

        builder.Property(h => h.IsArchived).HasDefaultValue(false);
    }
}