using api.JiApp.LovingBoards.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace api.JiApp.LovingBoards.Persistence;

public interface ILovingBoardsDbContext
{
    DbSet<Board> Boards { get; }
    DbSet<BoardItem> BoardItems { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
