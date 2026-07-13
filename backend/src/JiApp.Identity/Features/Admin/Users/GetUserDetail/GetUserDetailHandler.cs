using System.Globalization;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using Microsoft.AspNetCore.Identity;

namespace JiApp.Identity.Features.Admin.Users.GetUserDetail;

public sealed class GetUserDetailHandler(UserManager<User> userManager)
{
	public async Task<Result<GetUserDetailResponse>> HandleAsync(long userId, CancellationToken ct)
	{
		var user = await userManager.FindByIdAsync(userId.ToString(CultureInfo.InvariantCulture));
		if (user is null)
			return Result<GetUserDetailResponse>.Failure($"User with ID {userId} not found", ResultCategories.NotFound);

		var roles = await userManager.GetRolesAsync(user);
		var isLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

		return Result<GetUserDetailResponse>.Success(new GetUserDetailResponse(
			user.Id, user.UserName, user.Email, user.DisplayName, [.. roles], isLockedOut, user.LockoutEnd));
	}
}
