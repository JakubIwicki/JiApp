using System.Globalization;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Configuration;
using JiApp.Identity.Logging;
using JiApp.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace JiApp.Identity.Features.Auth.Refresh;

public sealed class RefreshHandler(
    IRefreshTokenService refreshTokenService,
    UserManager<User> userManager,
    IJwtTokenService jwtTokenService,
    IUserAccessService accessService,
    IdentitySettings settings,
    ILogger<RefreshHandler> logger)
{
    public async Task<Result<RefreshResponse>> HandleAsync(RefreshRequest request, CancellationToken ct)
    {
        var storedToken = await refreshTokenService.ValidateAsync(request.RefreshToken, ct);
        if (storedToken is null)
        {
            logger.RefreshTokenInvalid();
            return Result<RefreshResponse>.Failure("Invalid or expired refresh token");
        }

        // Fast path: if the submitted token was already revoked, this is potential token theft.
        // Revoke all tokens for the user immediately without further processing.
        if (storedToken.IsRevoked)
        {
            logger.RefreshTokenReuseDetected(storedToken.Id, storedToken.UserId);
            await refreshTokenService.RevokeAllForUserAsync(storedToken.UserId, ct);
            return Result<RefreshResponse>.Failure("Invalid or expired refresh token");
        }

        var user = await userManager.FindByIdAsync(storedToken.UserId.ToString(CultureInfo.InvariantCulture));
        if (user is null)
            return Result<RefreshResponse>.Failure("User not found");

        logger.RefreshTokenValidated(storedToken.UserId);

        // Atomic revoke+create inside a transaction to prevent token multiplication
        // from concurrent requests. RevokeAsync uses ExecuteUpdateAsync and returns
        // true only if a row was actually modified. If another concurrent request
        // already revoked this token, RevokeAsync returns false — treat as token theft.
        await using var transaction = await refreshTokenService.BeginTransactionAsync(ct);

        var wasRevoked = await refreshTokenService.RevokeAsync(storedToken.Id, ct);
        if (!wasRevoked)
        {
            logger.RefreshTokenReuseDetected(storedToken.Id, storedToken.UserId);
            await refreshTokenService.RevokeAllForUserAsync(storedToken.UserId, ct);
            await transaction.RollbackAsync(ct);
            return Result<RefreshResponse>.Failure("Invalid or expired refresh token");
        }

        logger.RefreshTokenRevoked(storedToken.Id);

        if (user.SecurityStamp is null)
            await userManager.UpdateSecurityStampAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        var permissions = await accessService.GetEffectivePermissionsAsync(user.Id);
        var accessToken = jwtTokenService.GenerateToken(user.Id, user.UserName!, roles, permissions, user.SecurityStamp!);
        var newRefreshToken = await refreshTokenService.CreateAsync(user.Id, ct);
        var expiresIn = settings.GetAccessTokenExpireMinutes() * 60;

        await transaction.CommitAsync(ct);

        return Result<RefreshResponse>.Success(new RefreshResponse(accessToken, newRefreshToken.Token, expiresIn));
    }
}
