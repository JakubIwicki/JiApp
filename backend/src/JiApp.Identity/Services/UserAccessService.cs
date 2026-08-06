using JiApp.Common.Constants;
using JiApp.Common.Models;
using JiApp.Identity.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Identity.Services;

public interface IUserAccessService
{
    Task AssignDefaultRoleAsync(long userId, CancellationToken ct = default);
    Task<string[]> GetEffectivePermissionsAsync(long userId, CancellationToken ct = default);
}

public sealed class UserAccessService(
    UserManager<User> userManager,
    IdentityDbContext db) : IUserAccessService
{
    public async Task AssignDefaultRoleAsync(long userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is not null)
            await userManager.AddToRoleAsync(user, RoleNames.Guest);
    }

    public async Task<string[]> GetEffectivePermissionsAsync(long userId, CancellationToken ct = default)
    {
        // One join resolves every permission claim across the user's role
        // assignments; a missing user has no user-role rows, so it yields [].
        var permissions = await db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(db.RoleClaims, ur => ur.RoleId, rc => rc.RoleId, (_, rc) => rc)
            .Where(rc => rc.ClaimType == Permissions.PermissionClaimType && rc.ClaimValue != null)
            .Select(rc => rc.ClaimValue!)
            .Distinct()
            .ToArrayAsync(ct);

        return permissions;
    }
}
