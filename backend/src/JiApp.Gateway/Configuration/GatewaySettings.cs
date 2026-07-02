using System;
using System.Collections.Generic;

namespace JiApp.Gateway.Configuration;

public sealed class GatewaySettings
{
    public JwtSettings? Jwt { get; set; }
    public string[]? CorsAllowedOrigins { get; set; }
    public Dictionary<string, RateLimitPolicyConfig>? RateLimiting { get; set; }

    public void Validate()
    {
        var errors = new List<string>();

        ValidateJwt(errors);
        ValidateRateLimiting(errors);

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

        if (string.IsNullOrEmpty(Jwt.Key))
            errors.Add("Jwt:Key is not configured.");
        else if (Jwt.Key.Length < 32)
            errors.Add("Jwt:Key must be at least 32 characters long.");
        if (string.IsNullOrEmpty(Jwt.Issuer))
            errors.Add("Jwt:Issuer is not configured.");
        if (string.IsNullOrEmpty(Jwt.Audience))
            errors.Add("Jwt:Audience is not configured.");
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
            "Me", "GetDownloadLink", "Preview", "Scheduler", "LovingBoards", "Assistant"
        };

        foreach (var policy in expectedPolicies)
        {
            if (!RateLimiting.ContainsKey(policy))
                errors.Add($"RateLimiting:{policy} is not configured.");
        }
    }

    [Serializable]
    public sealed class JwtSettings
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class RateLimitPolicyConfig
    {
        public int PermitLimit { get; set; }
        public int WindowInSeconds { get; set; }
        public int QueueLimit { get; set; }
        public int SegmentsPerWindow { get; set; }
    }
}