using JiApp.Common.Authentication;

namespace JiApp.Scheduler.Configuration;

[Serializable]
public sealed class SchedulerSettings
{
    public string? ConnectionString { get; set; }
    public JwtSettings? Jwt { get; set; }
    public string[]? CorsAllowedOrigins { get; set; }
    public string? IdentityBaseUrl { get; set; }
    public int DefaultPageSize { get; set; } = 50;
    public int MaxPageSize { get; set; } = 100;

    public int ClampTake(int? take) => Math.Clamp(take ?? DefaultPageSize, 1, MaxPageSize);

    public void Validate()
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(ConnectionString))
            errors.Add("ConnectionString is required");

        if (Jwt is null)
            errors.Add("Jwt section is required");
        else
            errors.AddRange(Jwt.Validate());

        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"SchedulerSettings validation failed: {string.Join("; ", errors)}");
    }
}
