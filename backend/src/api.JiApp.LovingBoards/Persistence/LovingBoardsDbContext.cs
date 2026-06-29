using api.JiApp.LovingBoards.Domain;
using api.JiApp.LovingBoards.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace api.JiApp.LovingBoards.Persistence;

public sealed class LovingBoardsDbContext(DbContextOptions<LovingBoardsDbContext> options)
    : DbContext(options), ILovingBoardsDbContext
{
    public DbSet<Board> Boards => Set<Board>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BoardConfiguration());
    }
}
