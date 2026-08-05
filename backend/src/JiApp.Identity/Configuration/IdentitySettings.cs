using JiApp.Common.Authentication;

namespace JiApp.Identity.Configuration;

[Serializable]
public sealed class IdentitySettings
{
    public string? ConnectionString { get; set; }
    public JwtSettings? Jwt { get; set; }
    public string[]? CorsAllowedOrigins { get; set; }
    public BootstrapSettings? Bootstrap { get; set; }
    public Dictionary<string, RateLimitPolicyConfig>? RateLimiting { get; set; }

    public void Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(ConnectionString))
            errors.Add("ConnectionString is not configured.");

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

        errors.AddRange(Jwt.Validate());

        if (!Jwt.AccessTokenExpireMinutes.HasValue)
            errors.Add("Jwt:AccessTokenExpireMinutes is not configured.");
        else if (Jwt.AccessTokenExpireMinutes.Value <= 0)
            errors.Add("Jwt:AccessTokenExpireMinutes must be greater than 0.");
        if (!Jwt.RefreshTokenExpireDays.HasValue)
            errors.Add("Jwt:RefreshTokenExpireDays is not configured.");
        else if (Jwt.RefreshTokenExpireDays.Value <= 0)
            errors.Add("Jwt:RefreshTokenExpireDays must be greater than 0.");
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
            "Login", "Register", "Refresh", "Logout"
        };

        foreach (var policy in expectedPolicies)
        {
            if (!RateLimiting.ContainsKey(policy))
                errors.Add($"RateLimiting:{policy} is not configured.");
        }
    }

    public JwtSettings GetRequiredJwt() =>
        Jwt ?? throw new InvalidOperationException("JWT settings not configured. Call Validate() first.");

    public int GetAccessTokenExpireMinutes() =>
        Jwt?.AccessTokenExpireMinutes ?? throw new InvalidOperationException("Jwt:AccessTokenExpireMinutes not configured. Call Validate() first.");

    [Serializable]
    public sealed class BootstrapSettings
    {
        public string? AdminUsername { get; set; }
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