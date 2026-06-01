using JiApp.Scheduler.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JiApp.Scheduler.Persistence.Configurations;

public sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.OwnsOne(x => x.Amount, price =>
        {
            price.Property(p => p.Amount).HasColumnName("Amount_Amount").HasColumnType("decimal(18,2)");
            price.Property(p => p.Currency).HasColumnName("Amount_Currency").HasMaxLength(3).HasDefaultValue("PLN");
        });
        builder.HasOne(x => x.Board).WithMany().HasForeignKey(x => x.BoardId);
    }
}