using System;
using System.Text.Json;
using System.Threading.Tasks;
using JiApp.Api.Logging;
using JiApp.Common.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JiApp.Api.Middleware;

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
            logger.UnhandledExceptionOccurred(ex);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = env.IsDevelopment()
                ? new ApiErrorResponse(Error: ex.Message, Details: ex.StackTrace)
                : new ApiErrorResponse(Error: "An unexpected error occurred");

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, ApiErrorResponse.JsonOptions));
        }
    }
}