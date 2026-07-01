using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Configuration;
using JiApp.Identity.Logging;
using JiApp.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace JiApp.Identity.Features.Auth.Login;

public sealed class LoginHandler(
    SignInManager<User> signInManager,
    UserManager<User> userManager,
    IJwtTokenService jwtTokenService,
    IRefreshTokenService refreshTokenService,
    IUserAccessService accessService,
    IPasswordHasher<User> passwordHasher,
    IdentitySettings settings,
    ILogger<LoginHandler> logger)
{
    public async Task<Result<LoginResponse>> HandleAsync(LoginRequest request)
    {
        logger.LoginAttempt(request.Username);

        var user = await userManager.FindByNameAsync(request.Username);
        if (user is null)
        {
            // Normalize timing to prevent user enumeration via timing side-channel.
            // Always run a password hash verification, even for unknown users.
            var dummyUser = new User();
            var dummyHash = passwordHasher.HashPassword(dummyUser, "__dummy_timing_fix__");
            passwordHasher.VerifyHashedPassword(dummyUser, dummyHash, request.Password);

            logger.LoginFailedUserNotFound(request.Username);
            return Result<LoginResponse>.Failure("Invalid username or password");
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            logger.LoginFailedAccountLocked(request.Username);
            return Result<LoginResponse>.Failure("Account is locked. Please try again later.");
        }

        if (!result.Succeeded)
        {
            logger.LoginFailedInvalidPassword(request.Username);
            return Result<LoginResponse>.Failure("Invalid username or password");
        }

        if (user.SecurityStamp is null)
            await userManager.UpdateSecurityStampAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        var permissions = await accessService.GetEffectivePermissionsAsync(user.Id);
        var accessToken = jwtTokenService.GenerateToken(user.Id, user.UserName!, roles, permissions, user.SecurityStamp!);
        var refreshToken = await refreshTokenService.CreateAsync(user.Id);
        var expiresIn = settings.GetAccessTokenExpireMinutes() * 60;

        logger.LoginSuccessful(request.Username);
        return Result<LoginResponse>.Success(new LoginResponse(
            user.Id, user.DisplayName, accessToken, refreshToken.Token, expiresIn, [.. roles], permissions));
    }
}
