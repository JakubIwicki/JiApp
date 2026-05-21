namespace JiApp.Api.Middleware;

public class RequestLoggingMiddleware(ILogger<RequestLoggingMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        //TODO: ???
        logger.LogInformation("Request {Method} {Path} started", context.Request.Method, context.Request.Path);

        await next(context);

        logger.LogInformation("Request {Method} {Path} finished with {StatusCode}",
            context.Request.Method, context.Request.Path, context.Response.StatusCode);
    }
}
