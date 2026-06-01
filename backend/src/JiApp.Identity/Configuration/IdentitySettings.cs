namespace JiApp.Identity.Configuration;

[Serializable]
public sealed class IdentitySettings
{
    public string? ConnectionString { get; set; }
    public JwtSettings? Jwt { get; set; }

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

        if (string.IsNullOrEmpty(Jwt.Key))
            errors.Add("Jwt:Key is not configured.");
        else if (Jwt.Key.Length < 32)
            errors.Add("Jwt:Key must be at least 32 characters long.");
        if (string.IsNullOrEmpty(Jwt.Issuer))
            errors.Add("Jwt:Issuer is not configured.");
        if (string.IsNullOrEmpty(Jwt.Audience))
            errors.Add("Jwt:Audience is not configured.");
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
    public sealed class JwtSettings
    {
        public string? Key { get; set; }
        public string? Issuer { get; set; }
        public string? Audience { get; set; }
        public int? AccessTokenExpireMinutes { get; set; }
        public int? RefreshTokenExpireDays { get; set; }

        public string ValidatedKey => Key ?? throw new InvalidOperationException("Jwt:Key not configured after validation");
        public string ValidatedIssuer => Issuer ?? throw new InvalidOperationException("Jwt:Issuer not configured after validation");
        public string ValidatedAudience => Audience ?? throw new InvalidOperationException("Jwt:Audience not configured after validation");
        public int ValidatedAccessTokenExpireMinutes => AccessTokenExpireMinutes ?? throw new InvalidOperationException("Jwt:AccessTokenExpireMinutes not configured after validation");
        public int ValidatedRefreshTokenExpireDays => RefreshTokenExpireDays ?? throw new InvalidOperationException("Jwt:RefreshTokenExpireDays not configured after validation");
    }
}