using System.Globalization;
using System.Linq;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace JiApp.Identity.Features.Auth.ChangePassword;

public sealed class ChangePasswordHandler(
    UserManager<User> userManager,
    ICurrentUserService currentUser,
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

        return Result<bool>.Success(true);
    }
}
