using System.Globalization;
using System.Linq;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace JiApp.Identity.Features.Auth.ChangePassword;

public sealed class ChangePasswordHandler(
    UserManager<User> userManager,
    ICurrentUserService currentUser,
    IRefreshTokenService refreshTokenService,
    ILogger<ChangePasswordHandler> logger)
{
    public async Task<Result<bool>> HandleAsync(ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var user = await userManager.FindByIdAsync(userId.ToString(CultureInfo.InvariantCulture));

        if (user is null)
        {
            logger.LogWarning("User not found for ID {UserId}", userId);
            return Result<bool>.Failure("User not found", ResultCategories.NotFound);
        }

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Password change failed for user {UserId}: {Errors}", userId, errors);
            return Result<bool>.Failure(errors, ResultCategories.Validation);
        }

        // Changing the password rotates the security stamp, killing outstanding access tokens,
        // but outstanding refresh tokens survive it. Evict them all so an attacker who holds
        // one cannot mint new access tokens. Security cleanup must complete even if the request
        // aborts — never cancel the revoke.
        await refreshTokenService.RevokeAllForUserAsync(user.Id, CancellationToken.None);

        return Result<bool>.Success(true);
    }
}
