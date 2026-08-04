using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace JiApp.Identity.Configuration;

public static class RateLimitPartitioning
{
    public static string GetPartitionKey(HttpContext context)
    {
        var subject = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (subject is not null)
            return $"user:{subject}";

        return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }
}
