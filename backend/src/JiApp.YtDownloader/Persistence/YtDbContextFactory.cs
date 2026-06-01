using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace JiApp.YtDownloader.Persistence;

public sealed class YtDbContextFactory : IDesignTimeDbContextFactory<YtDbContext>
{
    public YtDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = configuration.GetConnectionString("YtDownloader")
                               ?? configuration["ConnectionString"]
                               ?? throw new InvalidOperationException("ConnectionString is not configured.");

        var optionsBuilder = new DbContextOptionsBuilder<YtDbContext>();
        optionsBuilder.UseSqlite(connectionString);
        return new YtDbContext(optionsBuilder.Options);
    }
}