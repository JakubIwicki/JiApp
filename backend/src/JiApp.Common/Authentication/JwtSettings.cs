namespace JiApp.Common.Authentication;

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

    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Key))
            errors.Add("Jwt:Key is not configured.");
        else if (Key.Length < 32)
            errors.Add("Jwt:Key must be at least 32 characters long.");
        if (string.IsNullOrWhiteSpace(Issuer))
            errors.Add("Jwt:Issuer is not configured.");
        if (string.IsNullOrWhiteSpace(Audience))
            errors.Add("Jwt:Audience is not configured.");

        return errors;
    }
}
