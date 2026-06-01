using Microsoft.AspNetCore.Http;

namespace JiApp.Common.Abstractions;

public static class ResultExtensions
{
    public static IResult ValidationError(this IResultExtensions extensions, string[] errors)
        => Results.ValidationProblem(new Dictionary<string, string[]> { ["errors"] = errors });
}