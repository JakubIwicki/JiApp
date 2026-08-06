using System.Security.Claims;
using JiApp.Common;
using JiApp.Common.Abstractions;
using JiApp.Common.Constants;
using Microsoft.AspNetCore.Identity;

namespace JiApp.Identity.Features.Admin.Roles.ListRoles;

public sealed class ListRolesHandler(RoleManager<IdentityRole<long>> roleManager)
{
    public async Task<Result<ListRolesResponse>> HandleAsync(CancellationToken ct)
    {
        var roles = roleManager.Roles.ToList();
        var summaries = new List<RoleSummary>(roles.Count);

        foreach (var role in roles)
        {
            var roleName = role.Name ?? string.Empty;
            var claims = await roleManager.GetClaimsAsync(role);
            var permissions = claims
                .Where(c => c.Type == Permissions.PermissionClaimType)
                .Select(c => c.Value)
                .ToList();
            summaries.Add(new RoleSummary(roleName, permissions));
        }

        return Result<ListRolesResponse>.Success(new ListRolesResponse([.. summaries]));
    }
}
