using System.Text.Json;
using JiApp.Common.Abstractions;

namespace JiApp.Gateway.RateLimiting;

public sealed class RateLimitPolicySelector(RequestDelegate next, RateLimitPolicyService policyService)
{
    // Maps request paths to rate limit policy names.
    // Must match the policy names expected by GatewaySettings.Validate().
    // null = no rate limiting applied (passthrough for proxied routes without specific limits).
    // Paths not in this map → 403 Forbidden (fail closed — no silent degradation).
    private static readonly Dictionary<string, string?> PathPolicyMap = new()
    {
        // Auth
        ["/api/v1/auth/login"] = RateLimitPolicyNames.LoginPolicy,
        ["/api/v1/auth/register"] = RateLimitPolicyNames.RegisterPolicy,
        ["/api/v1/auth/refresh"] = RateLimitPolicyNames.RefreshPolicy,
        ["/api/v1/auth/logout"] = RateLimitPolicyNames.LogoutPolicy,
        ["/api/v1/auth/me"] = RateLimitPolicyNames.MePolicy,
        ["/api/v1/auth/profile"] = RateLimitPolicyNames.LoginPolicy,
        ["/api/v1/auth/change-password"] = RateLimitPolicyNames.LoginPolicy,
        ["/api/v1/auth/throw"] = RateLimitPolicyNames.ThrowPolicy,

        // YT search
        ["/api/v1/yt/search"] = RateLimitPolicyNames.SearchVideosPolicy,

        // YT downloads
        ["/api/v1/yt/downloads/mp3"] = RateLimitPolicyNames.GetDownloadLinkPolicy,
        ["/api/v1/yt/downloads/file"] = RateLimitPolicyNames.DownloadFilePolicy,

        // YT history
        ["/api/v1/yt/search/history"] = RateLimitPolicyNames.SearchHistoryPolicy,
        ["/api/v1/yt/downloads/history"] = RateLimitPolicyNames.DownloadHistoryPolicy,
        ["/api/v1/yt/history"] = RateLimitPolicyNames.GetHistoryPolicy,

        // YT preview
        ["/api/v1/yt/preview"] = RateLimitPolicyNames.PreviewPolicy,

        // YT assistant (LLM chat, SSE)
        ["/api/v1/yt/assistant/chat"] = RateLimitPolicyNames.AssistantPolicy,

        // Health
        ["/health"] = RateLimitPolicyNames.HealthPolicy,
        ["/health/live"] = RateLimitPolicyNames.HealthPolicy,
        ["/health/ready"] = RateLimitPolicyNames.HealthPolicy,

        // Downstream service health checks
        ["/api/v1/auth/health"] = RateLimitPolicyNames.HealthPolicy,
        ["/api/v1/yt/health"] = RateLimitPolicyNames.HealthPolicy,
        ["/api/v1/imagetools/health"] = RateLimitPolicyNames.HealthPolicy,
        ["/api/v1/scheduler/health"] = RateLimitPolicyNames.HealthPolicy,
        ["/api/v1/lovingboards/health"] = RateLimitPolicyNames.HealthPolicy,

        // Proxied routes without specific rate limits (YARP catch-all).
        // Pass through with no rate limiting applied.
        ["/api/v1/imagetools"] = null,
        ["/api/v1/scheduler"] = RateLimitPolicyNames.SchedulerPolicy,
        ["/api/v1/lovingboards"] = RateLimitPolicyNames.LovingBoardsPolicy,
    };

    // Longest-prefix-first ordering ensures more specific paths (e.g.
    // /api/v1/yt/search/history) match before shorter prefixes.
    private static readonly KeyValuePair<string, string?>[] PrefixMap =
    [
        .. PathPolicyMap
            .Where(kvp => !kvp.Key.EndsWith('/'))
            .OrderByDescending(kvp => kvp.Key.Length)
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        var path = (context.Request.Path.Value ?? "").ToLowerInvariant();

        // 1) Exact match
        if (PathPolicyMap.TryGetValue(path, out var policyName))
        {
            if (policyName is not null)
                AttachRateLimitPolicyToContext(context, policyName);

            await next(context);
            return;
        }

        // 2) Prefix match (longest first)
        foreach (var (prefix, policy) in PrefixMap)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                if (policy is not null)
                    AttachRateLimitPolicyToContext(context, policy);

                await next(context);
                return;
            }
        }

        // 3) No policy configured for this path — check whether routing matched a route
        if (context.GetEndpoint() is null)
        {
            // No route endpoint was resolved — let the pipeline return 404
            await next(context);
            return;
        }

        // 4) Fail closed — route endpoint exists but no rate limit policy configured
        await WriteForbiddenResponse(context);
    }

    private void AttachRateLimitPolicyToContext(HttpContext context, string policyName)
    {
        var originalEndpoint = context.GetEndpoint();

        var newEndpoint = originalEndpoint is not null
            ? policyService.AttachRateLimitPolicy(originalEndpoint, policyName)
            : policyService.CreatePolicyEndpoint(context.Request.Path.Value ?? "", policyName);

        context.SetEndpoint(newEndpoint);
    }

    private static async Task WriteForbiddenResponse(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        var error = new ApiErrorResponse("No rate limit policy configured for this endpoint");
        await JsonSerializer.SerializeAsync(context.Response.Body, error, ApiErrorResponse.JsonOptions);
    }
}