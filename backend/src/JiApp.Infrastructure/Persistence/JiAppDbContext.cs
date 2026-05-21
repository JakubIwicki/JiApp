using JiApp.Common.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Infrastructure.Persistence;

public class JiAppDbContext(DbContextOptions<JiAppDbContext> options)
    : IdentityDbContext<User, IdentityRole<long>, long>(options)
{
    public DbSet<EventLog> EventLogs => Set<EventLog>();
    public DbSet<YoutubeSearchHistory> YoutubeSearchHistory => Set<YoutubeSearchHistory>();
    public DbSet<YoutubeDownloadHistory> YoutubeDownloadHistory => Set<YoutubeDownloadHistory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(System.Reflection.Assembly.GetExecutingAssembly());
    }
}
