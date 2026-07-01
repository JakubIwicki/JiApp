using System.Globalization;
using JiApp.Common;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Common;
using Microsoft.AspNetCore.Identity;

namespace JiApp.Identity.Features.Admin.Users.AssignRole;

public sealed class AssignRoleHandler(
	UserManager<User> userManager,
	RoleManager<IdentityRole<long>> roleManager,
	AdminAccessGuard guard)
{
	public async Task<Result<bool>> HandleAsync(long userId, AssignRoleRequest request)
	{
		if (request.RoleName == RoleNames.Admin)
		{
			var notSelf = guard.EnsureNotSelf(userId);
			if (!notSelf.IsSuccess)
				return notSelf;
		}

		if (!await roleManager.RoleExistsAsync(request.RoleName))
			return Result<bool>.Failure($"Role '{request.RoleName}' does not exist", ResultCategories.Validation);

		var user = await userManager.FindByIdAsync(userId.ToString(CultureInfo.InvariantCulture));
		if (user is null)
			return Result<bool>.Failure($"User with ID {userId} not found", ResultCategories.NotFound);

		var result = await userManager.AddToRoleAsync(user, request.RoleName);
		if (!result.Succeeded)
		{
			var errors = string.Join(", ", result.Errors.Select(e => e.Description));
			return Result<bool>.Failure(errors, ResultCategories.Validation);
		}

		return Result<bool>.Success(true);
	}
}
