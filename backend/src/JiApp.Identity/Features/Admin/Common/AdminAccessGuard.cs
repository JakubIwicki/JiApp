using System.Globalization;
using JiApp.Common;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using Microsoft.AspNetCore.Identity;

namespace JiApp.Identity.Features.Admin.Common;

public sealed class AdminAccessGuard(
	UserManager<User> userManager,
	ICurrentUserService currentUser)
{
	public Result<bool> EnsureNotSelf(long targetUserId)
	{
		if (targetUserId == currentUser.UserId)
			return Result<bool>.Failure("Cannot perform this action on your own account", ResultCategories.AccessDenied);

		return Result<bool>.Success(true);
	}

	public async Task<Result<bool>> EnsureNotLastAdminAsync(long targetUserId)
	{
		var user = await userManager.FindByIdAsync(targetUserId.ToString(CultureInfo.InvariantCulture));
		if (user is null || !await userManager.IsInRoleAsync(user, RoleNames.Admin))
			return Result<bool>.Success(true);

		var admins = await userManager.GetUsersInRoleAsync(RoleNames.Admin);
		var effectiveAdmins = new List<User>(admins.Count);
		foreach (var admin in admins)
		{
			if (!await userManager.IsLockedOutAsync(admin))
				effectiveAdmins.Add(admin);
		}

		if (effectiveAdmins.Count <= 1)
			return Result<bool>.Failure(
				"Cannot remove or disable the last admin user. Assign another admin first.",
				ResultCategories.AccessDenied);

		return Result<bool>.Success(true);
	}

	public Result<bool> EnsureRoleIsEditable(string roleName)
	{
		if (roleName == RoleNames.Admin)
			return Result<bool>.Failure(
				"The Admin role's permissions are immutable and cannot be edited.",
				ResultCategories.AccessDenied);

		return Result<bool>.Success(true);
	}

	public Result<bool> EnsureRoleIsDeletable(string roleName)
	{
		if (roleName is RoleNames.Admin or RoleNames.User or RoleNames.Guest)
			return Result<bool>.Failure(
				$"The '{roleName}' role is reserved and cannot be deleted.",
				ResultCategories.AccessDenied);

		return Result<bool>.Success(true);
	}
}
