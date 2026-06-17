using JiApp.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JiApp.YtDownloader.Persistence.Configurations;

public sealed class AssistantDailyUsageConfiguration : IEntityTypeConfiguration<AssistantDailyUsage>
{
    public void Configure(EntityTypeBuilder<AssistantDailyUsage> builder)
    {
        builder.ToTable("AssistantDailyUsage");

        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.HasIndex(e => new { e.UserId, e.UsageDateUtc })
            .IsUnique();

        builder.HasIndex(e => e.UserId);
    }
}
