using System.Text.Json;
using JiApp.Common.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
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
        catch (BadHttpRequestException ex)
        {
            if (context.Response.HasStarted)
                throw;
            // Missing required query/route parameter, invalid body, etc. → 400
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            var response = env.IsDevelopment()
                ? new ApiErrorResponse(ex.Message)
                : new ApiErrorResponse("Invalid request");

            await JsonSerializer.SerializeAsync(context.Response.Body, response, ApiErrorResponse.JsonOptions);
        }
        catch (UnauthorizedAccessException)
        {
            // CurrentUserService throws this when the identity claim is missing or invalid —
            // a client error, so return 401, not 500. Must precede the generic catch below,
            // whose filter matches every Exception except OperationCanceledException.
            if (context.Response.HasStarted)
                throw;
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            var response = new ApiErrorResponse("Unauthorized");
            await JsonSerializer.SerializeAsync(context.Response.Body, response, ApiErrorResponse.JsonOptions);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Unhandled exception occurred");

            // If the response has already started (streaming endpoint failed mid-stream),
            // writing status/body throws InvalidOperationException and masks the real error.
            // Let the host abort the stream.
            if (context.Response.HasStarted)
                throw;

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = env.IsDevelopment()
                ? new ApiErrorResponse(ex.Message, ex.StackTrace)
                : new ApiErrorResponse("An unexpected error occurred");

            await JsonSerializer.SerializeAsync(context.Response.Body, response, ApiErrorResponse.JsonOptions);
        }
    }
}