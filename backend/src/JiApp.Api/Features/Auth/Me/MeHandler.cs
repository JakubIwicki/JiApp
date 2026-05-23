using System.Globalization;
using System.Threading.Tasks;
using JiApp.Api.Logging;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace JiApp.Api.Features.Auth.Me;

internal sealed class MeHandler(
    UserManager<User> userManager,
    ICurrentUserService currentUser,
    ILogger<MeHandler> logger)
{
    public async Task<Result<MeResponse>> HandleAsync()
    {
        logger.FetchingCurrentUser();

        var userId = currentUser.UserId;
        var user = await userManager.FindByIdAsync(userId.ToString(CultureInfo.InvariantCulture));

        if (user is null)
        {
            logger.UserNotFoundForId(userId);
            return Result<MeResponse>.Failure("User not found");
        }

        return Result<MeResponse>.Success(new MeResponse(user.Id, user.DisplayName));
    }
}