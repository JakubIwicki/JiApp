using JiApp.Scheduler.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JiApp.Scheduler.Persistence.Configurations;

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Location).HasMaxLength(200);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.OwnsOne(x => x.Price, price =>
        {
            price.Property(p => p.Amount).HasColumnName("Price_Amount").HasColumnType("decimal(18,2)");
            price.Property(p => p.Currency).HasColumnName("Price_Currency").HasMaxLength(3).HasDefaultValue("PLN");
        });
        builder.HasOne(x => x.Client).WithMany(c => c.Appointments).HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Board).WithMany().HasForeignKey(x => x.BoardId);
    }
}