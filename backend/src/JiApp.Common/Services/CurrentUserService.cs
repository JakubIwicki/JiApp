using System.Security.Claims;
using JiApp.Common.Abstractions;
using Microsoft.AspNetCore.Http;

namespace JiApp.Common.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private long? _userId;
    private string? _username;

    public long UserId => _userId ??= EvaluateUserId();

    public string Username => _username ??= EvaluateUsername();

    private long EvaluateUserId()
    {
        var claim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (claim is null || !long.TryParse(claim, out var userId))
            throw new UnauthorizedAccessException("User identity claim is missing or invalid");
        return userId;
    }

    private string EvaluateUsername()
    {
        return httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
    }
}