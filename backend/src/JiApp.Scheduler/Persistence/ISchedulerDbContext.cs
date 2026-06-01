using JiApp.Scheduler.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace JiApp.Scheduler.Persistence;

public interface ISchedulerDbContext
{
    DbSet<Board> Boards { get; }
    DbSet<Client> Clients { get; }
    DbSet<Service> Services { get; }
    DbSet<Appointment> Appointments { get; }
    DbSet<Expense> Expenses { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}