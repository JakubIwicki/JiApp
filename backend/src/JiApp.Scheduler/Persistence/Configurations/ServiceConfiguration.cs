using JiApp.Scheduler.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JiApp.Scheduler.Persistence.Configurations;

public sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(50);
        builder.OwnsOne(x => x.BasePrice, price =>
        {
            price.Property(p => p.Amount).HasColumnName("BasePrice_Amount").HasColumnType("decimal(18,2)");
            price.Property(p => p.Currency).HasColumnName("BasePrice_Currency").HasMaxLength(3).HasDefaultValue("PLN");
        });
        builder.HasOne(x => x.Board).WithMany().HasForeignKey(x => x.BoardId);
    }
}