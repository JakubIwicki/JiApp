using JiApp.Common;
using JiApp.Common.Models;
using JiApp.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Identity.Services;

public interface IUserModuleGrantService
{
    Task GrantAllAsync(long userId);
    Task<string[]> GetModulesAsync(long userId);
}

public sealed class UserModuleGrantService(IdentityDbContext dbContext) : IUserModuleGrantService
{
    public async Task GrantAllAsync(long userId)
    {
        var existing = await dbContext.UserModuleGrants
            .Where(g => g.UserId == userId)
            .Select(g => g.ModuleName)
            .ToListAsync();

        var missing = Modules.All.Where(module => !existing.Contains(module));

        foreach (var module in missing)
        {
            dbContext.UserModuleGrants.Add(new UserModuleGrant
            {
                UserId = userId,
                ModuleName = module,
                GrantedAt = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task<string[]> GetModulesAsync(long userId)
    {
        return await dbContext.UserModuleGrants
            .AsNoTracking()
            .Where(g => g.UserId == userId)
            .Select(g => g.ModuleName)
            .ToArrayAsync();
    }
}
