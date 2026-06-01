using System.Globalization;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace JiApp.Identity.Features.Auth.Me;

public sealed class MeHandler(
    UserManager<User> userManager,
    ICurrentUserService currentUser,
    ILogger<MeHandler> logger)
{
    public async Task<Result<MeResponse>> HandleAsync()
    {
        logger.FetchingCurrentUser();

        var userId = currentUser.UserId;
        var username = currentUser.Username;
        var user = await userManager.FindByIdAsync(userId.ToString(CultureInfo.InvariantCulture));

        if (user is null)
        {
            logger.UserNotFoundForId(userId);
            return Result<MeResponse>.Failure("User not found");
        }

        return Result<MeResponse>.Success(new MeResponse(user.Id, user.DisplayName, username));
    }
}
