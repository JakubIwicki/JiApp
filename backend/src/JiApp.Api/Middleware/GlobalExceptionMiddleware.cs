using System.Text.Json;
using JiApp.Common.Abstractions;

namespace JiApp.Api.Middleware;

public class GlobalExceptionMiddleware : IMiddleware
{
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionMiddleware(ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Unhandled exception occurred");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = _env.IsDevelopment()
                ? new ApiErrorResponse(Error: ex.Message, Details: ex.StackTrace)
                : new ApiErrorResponse(Error: "An unexpected error occurred");

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, ApiErrorResponse.JsonOptions));
        }
    }
}
