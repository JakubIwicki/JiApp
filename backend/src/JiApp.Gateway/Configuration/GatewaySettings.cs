using System;
using System.Collections.Generic;
using JiApp.Common.Authentication;

namespace JiApp.Gateway.Configuration;

public sealed class GatewaySettings
{
    public JwtSettings? Jwt { get; set; }
    public string[]? CorsAllowedOrigins { get; set; }
    public Dictionary<string, RateLimitPolicyConfig>? RateLimiting { get; set; }

    /// <summary>
    /// Upper bound for the rate-limit endpoint cache
    /// (<see cref="JiApp.Gateway.RateLimiting.RateLimitPolicyService"/>).
    /// Scalar on this settings root; override via the <c>EndpointCacheMaxEntries</c> config key
    /// (defaults to 4096 so no config is required).
    /// </summary>
    public int EndpointCacheMaxEntries { get; set; } = 4096;

    public void Validate(IWebHostEnvironment? env = null)
    {
        var errors = new List<string>();

        ValidateJwt(errors);
        ValidateRateLimiting(errors);

        // Development uses the any-origin fallback; anything else (including an unspecified
        // environment) must configure the allow-list explicitly — fail closed by default
        // (matches the AddCors fail-closed semantics).
        if (env is null || !env.IsDevelopment())
            ValidateCorsAllowedOrigins(errors);

        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"Configuration validation failed:\n{string.Join("\n", errors)}");
    }

    private void ValidateJwt(List<string> errors)
    {
        if (Jwt is null)
        {
            errors.Add("Jwt section is not configured.");
            return;
        }

        errors.AddRange(Jwt.Validate());
    }

    private void ValidateRateLimiting(List<string> errors)
    {
        if (RateLimiting is null or { Count: 0 })
        {
            errors.Add("RateLimiting section is not configured.");
            return;
        }

        var expectedPolicies = new[]
        {
            "Login", "Register", "Refresh", "Logout", "Health", "DownloadFile",
            "SearchVideos", "SearchHistory", "DownloadHistory", "GetHistory",
            "Me", "GetDownloadLink", "DownloadStatus", "Preview", "Scheduler", "LovingBoards", "Assistant"
        };

        foreach (var policy in expectedPolicies)
        {
            if (!RateLimiting.ContainsKey(policy))
                errors.Add($"RateLimiting:{policy} is not configured.");
        }
    }

    private void ValidateCorsAllowedOrigins(List<string> errors)
    {
        if (CorsAllowedOrigins is not { Length: > 0 })
        {
            errors.Add("CorsAllowedOrigins is required in non-Development environments");
            return;
        }

        foreach (var origin in CorsAllowedOrigins)
        {
            if (!IsValidOrigin(origin))
                errors.Add($"CorsAllowedOrigins entry '{origin}' is not a valid http(s) origin");
        }
    }

    private static bool IsValidOrigin(string origin) =>
        origin == "*" ||
        (Uri.TryCreate(origin, UriKind.Absolute, out var uri)
         && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
         && !string.IsNullOrEmpty(uri.Host)
         && uri.AbsolutePath is "" or "/"
         && string.IsNullOrEmpty(uri.Query)
         && string.IsNullOrEmpty(uri.Fragment));

    [Serializable]
    public sealed class RateLimitPolicyConfig
    {
        public int PermitLimit { get; set; }
        public int WindowInSeconds { get; set; }
        public int QueueLimit { get; set; }
        public int SegmentsPerWindow { get; set; }
    }
}