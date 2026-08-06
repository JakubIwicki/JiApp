using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace api.JiApp.LovingBoards.Clients;

/// <summary>
/// HTTP implementation of <see cref="IUserExistenceClient"/> against the Identity
/// service's <c>GET /api/v1/auth/users/{userId}/exists</c> endpoint, which requires
/// authentication. Mirrors <see cref="JiApp.Common.Services.RemoteSecurityStampValidator"/>:
/// the current request's Bearer token is forwarded verbatim; a request with no token
/// fails closed (never sends an unauthenticated probe), transport failures and timeouts
/// collapse to <see cref="UserExistenceStatus.Unavailable"/> (fail closed).
/// </summary>
public sealed class UserExistenceClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor,
    ILogger<UserExistenceClient> logger)
    : IUserExistenceClient
{
    public async Task<UserExistenceStatus> CheckExistsAsync(long userId, CancellationToken ct)
    {
        var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization;
        if (authHeader is not { Count: > 0 } header || string.IsNullOrEmpty(header[0]))
        {
            logger.LogWarning("User-existence probe: no Authorization header present on the current request");
            return UserExistenceStatus.Unavailable;
        }

        if (!header[0]!.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("User-existence probe: Authorization header is not a Bearer token");
            return UserExistenceStatus.Unavailable;
        }

        var authToken = header[0]!;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/auth/users/{userId}/exists");
            request.Headers.TryAddWithoutValidation("Authorization", authToken);

            using var response = await httpClient.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.OK)
                return UserExistenceStatus.Found;

            if (response.StatusCode == HttpStatusCode.NotFound)
                return UserExistenceStatus.NotFound;

            logger.LogWarning(
                "User-existence probe: Identity returned unexpected status {StatusCode}",
                (int)response.StatusCode);
            return UserExistenceStatus.Unavailable;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "User-existence probe: HTTP call to Identity failed");
            return UserExistenceStatus.Unavailable;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("User-existence probe: HTTP call to Identity timed out");
            return UserExistenceStatus.Unavailable;
        }
    }
}
