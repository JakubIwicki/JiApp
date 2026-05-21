using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;

namespace JiApp.Api.Features.Auth.Me;

public sealed class MeHandler(
    UserManager<User> userManager,
    ICurrentUserService currentUser)
{
    public async Task<Result<MeResponse>> HandleAsync()
    {
        var userId = currentUser.UserId;
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
            return Result<MeResponse>.Failure("User not found");

        return Result<MeResponse>.Success(new MeResponse(user.Id, user.DisplayName));
    }
}
