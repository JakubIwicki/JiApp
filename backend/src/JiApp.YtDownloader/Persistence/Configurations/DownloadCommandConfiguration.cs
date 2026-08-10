using JiApp.YtDownloader.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JiApp.YtDownloader.Persistence.Configurations;

public sealed class DownloadCommandConfiguration : IEntityTypeConfiguration<DownloadCommand>
{
    public void Configure(EntityTypeBuilder<DownloadCommand> builder)
    {
        builder.ToTable(TableNames.DownloadCommands);

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

        // At most one in-flight job per (UserId, VideoId): a double-tap on the same video
        // cannot enqueue a duplicate. Active rows are Queued, Processing, or Failed while
        // awaiting their retry backoff (NextAttemptAt set) — a completed row, or an
        // exhausted Failed row (dead letter), is a distinct, later download of the same video.
        builder.HasIndex(e => new { e.UserId, e.VideoId })
            .IsUnique()
            .HasFilter("\"Status\" IN ('Queued', 'Processing') OR (\"Status\" = 'Failed' AND \"NextAttemptAt\" IS NOT NULL)");
    }
}
