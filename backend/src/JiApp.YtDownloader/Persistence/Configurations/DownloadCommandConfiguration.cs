using JiApp.YtDownloader.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JiApp.YtDownloader.Persistence.Configurations;

public sealed class DownloadCommandConfiguration : IEntityTypeConfiguration<DownloadCommand>
{
    public void Configure(EntityTypeBuilder<DownloadCommand> builder)
    {
        builder.ToTable("DownloadCommands");

        builder.Property(e => e.Id).HasMaxLength(32);
        builder.Property(e => e.VideoId).HasMaxLength(150).IsRequired();
        builder.Property(e => e.VideoTitle).HasMaxLength(300).IsRequired();
        builder.Property(e => e.VideoDescription).HasMaxLength(1000);
        builder.Property(e => e.VideoImageUrl).HasMaxLength(300);
        builder.Property(e => e.VideoUrl).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.LastError).HasMaxLength(4000);
        builder.Property(e => e.ErrorCategory).HasMaxLength(50);
        builder.Property(e => e.FilePath).HasMaxLength(1024);

        builder.HasIndex(e => new { e.UserId, e.VideoId });
    }
}
