using System.Text.Json;

namespace JiApp.Common.Abstractions;

/// <summary>
/// Standard API error response used consistently across all error paths:
/// middleware, JWT challenge, rate limiter rejection, and endpoint error responses.
/// </summary>
public sealed record ApiErrorResponse(
    string Error,
    string? Details = null,
    string? RetryAfterSeconds = null)
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
