using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JiApp.Infrastructure.Persistence;

public class JiAppDbContextFactory : IDesignTimeDbContextFactory<JiAppDbContext>
{
    public JiAppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<JiAppDbContext>();
        optionsBuilder.UseSqlite("Data Source=dev-data/JiApp.db");

        return new JiAppDbContext(optionsBuilder.Options);
    }
}