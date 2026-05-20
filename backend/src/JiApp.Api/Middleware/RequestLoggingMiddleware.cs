namespace JiApp.Api.Middleware;

public class RequestLoggingMiddleware : IMiddleware
{
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(ILogger<RequestLoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        _logger.LogInformation("Request {Method} {Path} started", context.Request.Method, context.Request.Path);

        await next(context);

        _logger.LogInformation("Request {Method} {Path} finished with {StatusCode}",
            context.Request.Method, context.Request.Path, context.Response.StatusCode);
    }
}
