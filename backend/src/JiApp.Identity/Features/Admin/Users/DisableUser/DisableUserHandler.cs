using System.Globalization;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Common;
using JiApp.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace JiApp.Identity.Features.Admin.Users.DisableUser;

public sealed class DisableUserHandler(
	UserManager<User> userManager,
	IRefreshTokenService refreshTokenService,
	AdminAccessGuard guard,
	ILogger<DisableUserHandler> logger)
{
	public async Task<Result<bool>> HandleAsync(long userId, CancellationToken ct)
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

		await userManager.SetLockoutEnabledAsync(user, true);
		await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

		var stampResult = await userManager.UpdateSecurityStampAsync(user);
		if (!stampResult.Succeeded)
			logger.LogWarning("Failed to invalidate outstanding tokens for user {UserId} after account disable: {Errors}",
				user.Id, string.Join(", ", stampResult.Errors.Select(e => e.Description)));

		// Security cleanup must complete even if the request aborts — never cancel the revoke.
		await refreshTokenService.RevokeAllForUserAsync(userId, CancellationToken.None);

		return Result<bool>.Success(true);
	}
}
