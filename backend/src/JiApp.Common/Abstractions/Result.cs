namespace JiApp.Common.Abstractions;

public sealed record Result<T>(bool IsSuccess, T? Value, string? Error, string? ErrorCategory = null)
{
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error, string? errorCategory = null) => new(false, default, error, errorCategory);
}