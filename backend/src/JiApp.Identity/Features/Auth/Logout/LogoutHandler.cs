using JiApp.Common.Abstractions;
using JiApp.Identity.Logging;
using JiApp.Identity.Services;
using Microsoft.Extensions.Logging;

namespace JiApp.Identity.Features.Auth.Logout;

public sealed class LogoutHandler(
    IRefreshTokenService refreshTokenService,
    ILogger<LogoutHandler> logger)
{
    public async Task HandleAsync(LogoutRequest request, CancellationToken ct)
    {
        var storedToken = await refreshTokenService.ValidateAsync(request.RefreshToken, ct);
        if (storedToken is not null)
        {
            await refreshTokenService.RevokeAsync(storedToken.Id, ct);
            logger.RefreshTokenRevoked(storedToken.Id);
        }
    }
}
