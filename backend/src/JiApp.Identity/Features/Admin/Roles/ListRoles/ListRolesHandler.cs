using System.Security.Claims;
using JiApp.Common.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace JiApp.Identity.Features.Admin.Roles.ListRoles;

public sealed class ListRolesHandler(RoleManager<IdentityRole<long>> roleManager)
{
	public async Task<Result<ListRolesResponse>> HandleAsync()
	{
		var roles = roleManager.Roles.ToList();
		var summaries = new List<RoleSummary>(roles.Count);

		foreach (var role in roles)
		{
			var roleName = role.Name ?? string.Empty;
			var claims = await roleManager.GetClaimsAsync(role);
			var permissions = claims
				.Where(c => c.Type == "permission")
				.Select(c => c.Value)
				.ToList();
			summaries.Add(new RoleSummary(roleName, permissions));
		}

		return Result<ListRolesResponse>.Success(new ListRolesResponse([.. summaries]));
	}
}
