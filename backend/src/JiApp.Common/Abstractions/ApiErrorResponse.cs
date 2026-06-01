using System;
using System.Text.Json;

namespace JiApp.Common.Abstractions;

[Serializable]
public sealed record ApiErrorResponse(
    string Error,
    string? Details = null,
    string? RetryAfterSeconds = null)
{
    public const string UnknownErrorMessage = "An unknown error occurred";

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}