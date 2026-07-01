using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Common;
using Microsoft.AspNetCore.Identity;

namespace JiApp.Identity.Features.Admin.Roles.DeleteRole;

public sealed class DeleteRoleHandler(
	RoleManager<IdentityRole<long>> roleManager,
	UserManager<User> userManager,
	AdminAccessGuard guard)
{
	public async Task<Result<bool>> HandleAsync(string roleName)
	{
		var deletableCheck = guard.EnsureRoleIsDeletable(roleName);
		if (!deletableCheck.IsSuccess)
			return deletableCheck;

		var role = await roleManager.FindByNameAsync(roleName);
		if (role is null)
			return Result<bool>.Failure($"Role '{roleName}' not found", ResultCategories.NotFound);

		var usersInRole = await userManager.GetUsersInRoleAsync(roleName);
		if (usersInRole.Count > 0)
			return Result<bool>.Failure(
				$"Role '{roleName}' is assigned to {usersInRole.Count} user(s). Reassign them before deleting this role.",
				ResultCategories.Conflict);

		await roleManager.DeleteAsync(role);

		return Result<bool>.Success(true);
	}
}
