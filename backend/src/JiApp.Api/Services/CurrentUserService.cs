using System.Security.Claims;
using JiApp.Common.Abstractions;

namespace JiApp.Api.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public long UserId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(claim, out var userId) ? userId : 0;
        }
    }

    public string Username => httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
}
