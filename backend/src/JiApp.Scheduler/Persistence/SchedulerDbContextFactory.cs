using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace JiApp.Scheduler.Persistence;

public sealed class SchedulerDbContextFactory : IDesignTimeDbContextFactory<SchedulerDbContext>
{
    public SchedulerDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = configuration["ConnectionString"] ?? "Data Source=scheduler_dev.db";
        var builder = new DbContextOptionsBuilder<SchedulerDbContext>();
        builder.UseSqlite(connectionString);
        return new SchedulerDbContext(builder.Options);
    }
}