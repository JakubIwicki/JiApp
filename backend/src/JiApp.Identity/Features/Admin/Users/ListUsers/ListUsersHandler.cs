using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Identity.Features.Admin.Users.ListUsers;

public sealed class ListUsersHandler(UserManager<User> userManager, IdentityDbContext db)
{
    public async Task<Result<ListUsersResponse>> HandleAsync(string? search, int? page, int? pageSize, CancellationToken ct)
    {
        var p = Math.Max(1, page ?? 1);
        var ps = Math.Clamp(pageSize ?? 20, 1, 100);

        var query = userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                (u.UserName != null && u.UserName.Contains(term)) ||
                (u.Email != null && u.Email.Contains(term)));
        }

        var totalCount = await query.CountAsync(ct);
        var pagedUsers = await query.OrderBy(u => u.Id)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(ct);

        var userIds = pagedUsers.Select(u => u.Id).ToArray();
        var roleAssignments = await db.UserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .ToListAsync(ct);

        var rolesByUser = roleAssignments
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name!).ToArray());

        var summaries = pagedUsers.Select(user =>
        {
            var isLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
            return new UserSummary(user.Id, user.UserName, user.Email, user.DisplayName,
                rolesByUser.TryGetValue(user.Id, out var roles) ? roles : [], isLockedOut);
        }).ToList();

        return Result<ListUsersResponse>.Success(new ListUsersResponse([.. summaries], totalCount));
    }
}
