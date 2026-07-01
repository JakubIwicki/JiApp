using JiApp.Common;
using JiApp.Common.Models;
using Microsoft.AspNetCore.Identity;

namespace JiApp.Identity.Services;

public interface IUserAccessService
{
	Task AssignDefaultRoleAsync(long userId);
	Task<string[]> GetEffectivePermissionsAsync(long userId);
}

public sealed class UserAccessService(
	UserManager<User> userManager,
	RoleManager<IdentityRole<long>> roleManager) : IUserAccessService
{
	public async Task AssignDefaultRoleAsync(long userId)
	{
		var user = await userManager.FindByIdAsync(userId.ToString());
		if (user is not null)
			await userManager.AddToRoleAsync(user, RoleNames.Guest);
	}

	public async Task<string[]> GetEffectivePermissionsAsync(long userId)
	{
		var user = await userManager.FindByIdAsync(userId.ToString());
		if (user is null)
			return [];

		var roleNames = await userManager.GetRolesAsync(user);
		var permissions = new HashSet<string>();

		foreach (var roleName in roleNames)
		{
			var role = await roleManager.FindByNameAsync(roleName);
			if (role is null)
				continue;

			var claims = await roleManager.GetClaimsAsync(role);
			foreach (var claim in claims)
			{
				if (claim.Type == "permission")
					permissions.Add(claim.Value);
			}
		}

		return [.. permissions];
	}
}
