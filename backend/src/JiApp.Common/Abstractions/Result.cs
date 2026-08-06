namespace JiApp.Common.Abstractions;

public sealed record Result<T>(bool IsSuccess, T? Value, string? Error, string? ErrorCategory = null)
{
    public static Result<T> Success(T value) => new(true, value, null);

    public static Result<T> Failure(string error, string? errorCategory = null) =>
        new(false, default, error, errorCategory);
}

public sealed record Result(bool IsSuccess, string? Error, string? ErrorCategory = null)
{
    public static Result Success() => new(true, null);

    public static Result Failure(string error, string? errorCategory = null) =>
        new(false, error, errorCategory);
}

public static class ResultCategories
{
    public const string NotFound = "NotFound";
    public const string AccessDenied = "AccessDenied";
    public const string Validation = "Validation";
    public const string Conflict = "Conflict";
    public const string BadGateway = "BadGateway";
    public const string Unavailable = "Unavailable";
    public const string AccountLocked = "AccountLocked";
    public const string YoutubeDl = "YoutubeDl";
}