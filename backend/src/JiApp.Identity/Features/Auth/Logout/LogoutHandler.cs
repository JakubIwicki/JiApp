using JiApp.Common.Abstractions;
using JiApp.Identity.Logging;
using JiApp.Identity.Services;
using Microsoft.Extensions.Logging;

namespace JiApp.Identity.Features.Auth.Logout;

public sealed class LogoutHandler(
    IRefreshTokenService refreshTokenService,
    ILogger<LogoutHandler> logger)
{
    public async Task HandleAsync(LogoutRequest request)
    {
        var storedToken = await refreshTokenService.ValidateAsync(request.RefreshToken);
        if (storedToken is not null)
        {
            await refreshTokenService.RevokeAsync(storedToken.Id);
            logger.RefreshTokenRevoked(storedToken.Id);
        }
    }
}
