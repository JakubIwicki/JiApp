using System.Globalization;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace JiApp.Identity.Features.Admin.Users.EnableUser;

public sealed class EnableUserHandler(
	UserManager<User> userManager,
	AdminAccessGuard guard,
	ILogger<EnableUserHandler> logger)
{
	public async Task<Result<bool>> HandleAsync(long userId)
	{
		var notSelf = guard.EnsureNotSelf(userId);
		if (!notSelf.IsSuccess)
			return notSelf;

		var user = await userManager.FindByIdAsync(userId.ToString(CultureInfo.InvariantCulture));
		if (user is null)
			return Result<bool>.Failure($"User with ID {userId} not found", ResultCategories.NotFound);

		await userManager.SetLockoutEndDateAsync(user, null);

		var stampResult = await userManager.UpdateSecurityStampAsync(user);
		if (!stampResult.Succeeded)
			logger.LogWarning("Failed to invalidate outstanding tokens for user {UserId} after account enable: {Errors}",
				user.Id, string.Join(", ", stampResult.Errors.Select(e => e.Description)));

		return Result<bool>.Success(true);
	}
}
