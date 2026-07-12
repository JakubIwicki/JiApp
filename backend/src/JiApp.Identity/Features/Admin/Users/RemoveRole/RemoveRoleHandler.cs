using System.Globalization;
using JiApp.Common;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace JiApp.Identity.Features.Admin.Users.RemoveRole;

public sealed class RemoveRoleHandler(
	UserManager<User> userManager,
	AdminAccessGuard guard,
	ILogger<RemoveRoleHandler> logger)
{
	public async Task<Result<bool>> HandleAsync(long userId, string roleName, CancellationToken ct)
	{
		if (roleName == RoleNames.Admin)
		{
			var notLastAdmin = await guard.EnsureNotLastAdminAsync(userId);
			if (!notLastAdmin.IsSuccess)
				return notLastAdmin;
		}

		var user = await userManager.FindByIdAsync(userId.ToString(CultureInfo.InvariantCulture));
		if (user is null)
			return Result<bool>.Failure($"User with ID {userId} not found", ResultCategories.NotFound);

		var result = await userManager.RemoveFromRoleAsync(user, roleName);
		if (!result.Succeeded)
		{
			var errors = string.Join(", ", result.Errors.Select(e => e.Description));
			return Result<bool>.Failure(errors, ResultCategories.Validation);
		}

		var stampResult = await userManager.UpdateSecurityStampAsync(user);
		if (!stampResult.Succeeded)
			logger.LogWarning("Failed to invalidate outstanding tokens for user {UserId} after role removal: {Errors}",
				user.Id, string.Join(", ", stampResult.Errors.Select(e => e.Description)));

		return Result<bool>.Success(true);
	}
}
