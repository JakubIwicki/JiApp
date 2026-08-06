using Microsoft.AspNetCore.Http;

namespace JiApp.Common.Abstractions;

public static class ResultExtensions
{
    public static IResult ValidationError(this IResultExtensions extensions, string[] errors)
        => Results.ValidationProblem(new Dictionary<string, string[]> { ["errors"] = errors });

    public static Result<T> WithValue<T>(this Result result, T value) =>
        result.IsSuccess
            ? Result<T>.Success(value)
            : Result<T>.Failure(result.Error!, result.ErrorCategory);

    public static IResult ToHttp<T>(this Result<T> result)
    {
        if (result.IsSuccess) return Results.Ok(result.Value);
        var error = new ApiErrorResponse(result.Error ?? ApiErrorResponse.UnknownErrorMessage);
        return result.ErrorCategory switch
        {
            ResultCategories.NotFound => Results.NotFound(error),
            ResultCategories.AccessDenied => Results.NotFound(error),   // G3.2 policy
            ResultCategories.Validation => Results.BadRequest(error),
            ResultCategories.Conflict => Results.Conflict(error),
            ResultCategories.BadGateway => Results.Json(error, statusCode: 502),
            ResultCategories.Unavailable => Results.Json(error, statusCode: 503),
            _ => Results.Json(error, statusCode: 500)
        };
    }
}