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
    // Pre-computed IdentityV3 hash of a fixed dummy password. The known- and unknown-user
    // login paths must each perform exactly one KDF verify, otherwise response time reveals
    // whether the account exists. JiApp.Identity uses the default PasswordHasherOptions; if
    // those options are ever customized, compute this hash at the composition root from the
    // injected hasher instead of relying on this literal.
    private static readonly string DummyPasswordHash =
        "AQAAAAIAAYagAAAAEDAE8SCkaWrDpyZ2GsvQtTpNLdbg6mO59OHFvL7o81lkf0Gy72OE13ZttbAwjN4qJg==";

    public async Task<Result<LoginResponse>> HandleAsync(LoginRequest request, CancellationToken ct)
    {
        logger.LoginAttempt(request.Username);

        var user = await userManager.FindByNameAsync(request.Username);
        if (user is null)
        {
            // Timing oracle: the known-user path runs exactly one VerifyHashedPassword inside
            // CheckPasswordSignInAsync. A runtime HashPassword here would add a SECOND KDF pass,
            // making unknown users ~2x slower — the exact side channel this mitigation exists
            // to close. Verify against the pre-computed dummy hash instead.
            passwordHasher.VerifyHashedPassword(new User(), DummyPasswordHash, request.Password);

            logger.LoginFailedUserNotFound(request.Username);
            return Result<LoginResponse>.Failure("Invalid username or password");
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            logger.LoginFailedAccountLocked(request.Username);
            return Result<LoginResponse>.Failure(
                "Account is locked. Please try again later.",
                ResultCategories.AccountLocked);
        }

        if (!result.Succeeded)
        {
            logger.LoginFailedInvalidPassword(request.Username);
            return Result<LoginResponse>.Failure("Invalid username or password");
        }

        if (user.SecurityStamp is null)
            await userManager.UpdateSecurityStampAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        var permissions = await accessService.GetEffectivePermissionsAsync(user.Id, ct);
        var accessToken = jwtTokenService.GenerateToken(user.Id, user.UserName!, roles, permissions, user.SecurityStamp!);
        var refreshToken = await refreshTokenService.CreateAsync(user.Id, ct);
        var expiresIn = settings.GetAccessTokenExpireMinutes() * 60;

        logger.LoginSuccessful(request.Username);
        return Result<LoginResponse>.Success(new LoginResponse(
            user.Id, user.DisplayName, accessToken, refreshToken.Token, expiresIn, [.. roles], permissions));
    }
}
