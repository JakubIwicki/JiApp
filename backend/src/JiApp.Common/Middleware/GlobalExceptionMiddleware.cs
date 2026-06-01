using System.Text.Json;
using JiApp.Common.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JiApp.Common.Middleware;

public sealed class GlobalExceptionMiddleware(ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment env)
    : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Unhandled exception occurred");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = env.IsDevelopment()
                ? new ApiErrorResponse(ex.Message, ex.StackTrace)
                : new ApiErrorResponse("An unexpected error occurred");

            await JsonSerializer.SerializeAsync(context.Response.Body, response, ApiErrorResponse.JsonOptions);
        }
    }
}