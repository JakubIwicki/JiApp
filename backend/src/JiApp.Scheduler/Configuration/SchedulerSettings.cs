namespace JiApp.Scheduler.Configuration;

[Serializable]
public sealed class SchedulerSettings
{
    public string? ConnectionString { get; set; }
    public JwtSettings? Jwt { get; set; }
    public string[]? CorsAllowedOrigins { get; set; }

    public void Validate()
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(ConnectionString))
            errors.Add("ConnectionString is required");

        if (Jwt is null)
            errors.Add("Jwt section is required");
        else
            Jwt.Validate();

        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"SchedulerSettings validation failed: {string.Join("; ", errors)}");
    }
}

[Serializable]
public sealed class JwtSettings
{
    public string? Key { get; set; }
    public string? Issuer { get; set; }
    public string? Audience { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Key))
            throw new InvalidOperationException("Jwt:Key is required");
        if (string.IsNullOrWhiteSpace(Issuer))
            throw new InvalidOperationException("Jwt:Issuer is required");
        if (string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("Jwt:Audience is required");
    }
}