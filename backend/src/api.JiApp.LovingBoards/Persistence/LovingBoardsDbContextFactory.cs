using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace api.JiApp.LovingBoards.Persistence;

public sealed class LovingBoardsDbContextFactory : IDesignTimeDbContextFactory<LovingBoardsDbContext>
{
    public LovingBoardsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = configuration["ConnectionString"] ?? "Data Source=lovingboards_dev.db";
        var builder = new DbContextOptionsBuilder<LovingBoardsDbContext>();
        builder.UseSqlite(connectionString);
        return new LovingBoardsDbContext(builder.Options);
    }
}
