using JiApp.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace JiApp.YtDownloader.Persistence;

public sealed class YtDbContext(DbContextOptions<YtDbContext> options) : DbContext(options)
{
    public DbSet<YoutubeSearchHistory> YoutubeSearchHistory => Set<YoutubeSearchHistory>();
    public DbSet<YoutubeDownloadHistory> YoutubeDownloadHistory => Set<YoutubeDownloadHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(YtDbContext).Assembly);
    }
}