using System.Globalization;
using System.Linq;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace JiApp.Identity.Features.Auth.UpdateProfile;

public sealed class UpdateProfileHandler(
    UserManager<User> userManager,
    ICurrentUserService currentUser,
    ILogger<UpdateProfileHandler> logger)
{
    public async Task<Result<UpdateProfileResponse>> HandleAsync(UpdateProfileRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var user = await userManager.FindByIdAsync(userId.ToString(CultureInfo.InvariantCulture));

        if (user is null)
        {
            logger.LogWarning("User not found for ID {UserId}", userId);
            return Result<UpdateProfileResponse>.Failure("User not found", ResultCategories.NotFound);
        }

        user.DisplayName = request.DisplayName;

        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailResult = await userManager.SetEmailAsync(user, request.Email);
            if (!emailResult.Succeeded)
            {
                var errors = string.Join(", ", emailResult.Errors.Select(e => e.Description));
                logger.LogWarning("Email update failed for user {UserId}: {Errors}", userId, errors);
                var category = emailResult.Errors.Any(e =>
                    e.Code is "DuplicateEmail" or "DuplicateUserName")
                    ? ResultCategories.Conflict
                    : null;
                return Result<UpdateProfileResponse>.Failure(errors, category);
            }
        }

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            logger.LogWarning("Profile update failed for user {UserId}: {Errors}", userId, errors);
            return Result<UpdateProfileResponse>.Failure(errors);
        }

        return Result<UpdateProfileResponse>.Success(new UpdateProfileResponse(
            user.Id, user.DisplayName, user.UserName, user.Email));
    }
}
