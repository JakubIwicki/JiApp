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

    public void Validate(IWebHostEnvironment? env = null)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(ConnectionString))
            errors.Add("ConnectionString is required");

        if (Jwt is null)
            errors.Add("Jwt section is required");
        else
            errors.AddRange(Jwt.Validate());

        // Development uses the any-origin / NoOp fallbacks; anything else (including an
        // unspecified environment) must configure these explicitly — fail closed by default
        // (matches the AddCors / security-stamp fail-closed semantics).
        if (env is null || !env.IsDevelopment())
        {
            ValidateCorsAllowedOrigins(errors);
            ValidateIdentityBaseUrl(errors);
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"SchedulerSettings validation failed: {string.Join("; ", errors)}");
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

    private void ValidateIdentityBaseUrl(List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(IdentityBaseUrl))
        {
            errors.Add("IdentityBaseUrl is required in non-Development environments");
            return;
        }

        if (!Uri.TryCreate(IdentityBaseUrl, UriKind.Absolute, out _))
            errors.Add("IdentityBaseUrl must be a valid absolute URI");
    }

    private static bool IsValidOrigin(string origin) =>
        origin == "*" ||
        (Uri.TryCreate(origin, UriKind.Absolute, out var uri)
         && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
         && !string.IsNullOrEmpty(uri.Host)
         && uri.AbsolutePath is "" or "/"
         && string.IsNullOrEmpty(uri.Query)
         && string.IsNullOrEmpty(uri.Fragment));
}
