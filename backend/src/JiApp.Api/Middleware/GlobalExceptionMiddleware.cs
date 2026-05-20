using System.Text.Json;

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new Dictionary<string, string?>
            {
                ["error"] = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred"
            };
            if (_env.IsDevelopment())
            {
                response["details"] = ex.StackTrace;
            }

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
