using JiApp.Scheduler.Domain;
using JiApp.Scheduler.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Scheduler.Persistence;

public sealed class SchedulerDbContext(DbContextOptions<SchedulerDbContext> options)
    : DbContext(options), ISchedulerDbContext
{
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Expense> Expenses => Set<Expense>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BoardConfiguration());
        modelBuilder.ApplyConfiguration(new ClientConfiguration());
        modelBuilder.ApplyConfiguration(new ServiceConfiguration());
        modelBuilder.ApplyConfiguration(new AppointmentConfiguration());
        modelBuilder.ApplyConfiguration(new ExpenseConfiguration());
    }
}