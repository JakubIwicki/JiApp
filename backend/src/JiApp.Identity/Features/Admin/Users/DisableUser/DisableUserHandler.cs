using System.Globalization;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Common;
using JiApp.Identity.Services;
using Microsoft.AspNetCore.Identity;

namespace JiApp.Identity.Features.Admin.Users.DisableUser;

public sealed class DisableUserHandler(
	UserManager<User> userManager,
	IRefreshTokenService refreshTokenService,
	AdminAccessGuard guard)
{
	public async Task<Result<bool>> HandleAsync(long userId)
	{
		var notSelf = guard.EnsureNotSelf(userId);
		if (!notSelf.IsSuccess)
			return notSelf;

		var notLastAdmin = await guard.EnsureNotLastAdminAsync(userId);
		if (!notLastAdmin.IsSuccess)
			return notLastAdmin;

		var user = await userManager.FindByIdAsync(userId.ToString(CultureInfo.InvariantCulture));
		if (user is null)
			return Result<bool>.Failure($"User with ID {userId} not found", ResultCategories.NotFound);

		if (!await userManager.IsLockedOutAsync(user))
		{
			await userManager.SetLockoutEnabledAsync(user, true);
			await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
		}

		await refreshTokenService.RevokeAllForUserAsync(userId);

		return Result<bool>.Success(true);
	}
}
