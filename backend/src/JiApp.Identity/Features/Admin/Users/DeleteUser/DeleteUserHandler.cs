using System.Globalization;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Common;
using Microsoft.AspNetCore.Identity;

namespace JiApp.Identity.Features.Admin.Users.DeleteUser;

public sealed class DeleteUserHandler(
	UserManager<User> userManager,
	AdminAccessGuard guard)
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

		await userManager.DeleteAsync(user);

		return Result<bool>.Success(true);
	}
}
