using System.Globalization;
using System.Linq;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Services;
using Microsoft.AspNetCore.Identity;

namespace JiApp.Identity.Features.Admin.Users.ResetPassword;

public sealed class ResetPasswordHandler(
	UserManager<User> userManager,
	IRefreshTokenService refreshTokenService)
{
	public async Task<Result<bool>> HandleAsync(long userId, ResetPasswordRequest request, CancellationToken ct)
	{
		var user = await userManager.FindByIdAsync(userId.ToString(CultureInfo.InvariantCulture));
		if (user is null)
			return Result<bool>.Failure($"User with ID {userId} not found", ResultCategories.NotFound);

		var token = await userManager.GeneratePasswordResetTokenAsync(user);
		var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
		if (!result.Succeeded)
		{
			var errors = string.Join(", ", result.Errors.Select(e => e.Description));
			return Result<bool>.Failure(errors, ResultCategories.Validation);
		}

		await refreshTokenService.RevokeAllForUserAsync(userId, ct);

		return Result<bool>.Success(true);
	}
}
