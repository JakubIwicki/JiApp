using JiApp.Common.Authentication;

namespace JiApp.Identity.Configuration;

[Serializable]
public sealed class IdentitySettings
{
    public string? ConnectionString { get; set; }
    public JwtSettings? Jwt { get; set; }
    public string[]? CorsAllowedOrigins { get; set; }
    public BootstrapSettings? Bootstrap { get; set; }

    public void Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(ConnectionString))
            errors.Add("ConnectionString is not configured.");

        ValidateJwt(errors);

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

    public JwtSettings GetRequiredJwt() =>
        Jwt ?? throw new InvalidOperationException("JWT settings not configured. Call Validate() first.");

    public int GetAccessTokenExpireMinutes() =>
        Jwt?.AccessTokenExpireMinutes ?? throw new InvalidOperationException("Jwt:AccessTokenExpireMinutes not configured. Call Validate() first.");

    [Serializable]
    public sealed class BootstrapSettings
    {
        public string? AdminUsername { get; set; }
    }
}