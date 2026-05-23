using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using JiApp.Api.Configuration;
using JiApp.Common.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Api.Features.Downloads.GetDownloadLink;

public static class GetDownloadLinkEndpoint
{
    public static IEndpointRouteBuilder MapGetDownloadLink(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/downloads/mp3", async (
            DownloadRequest request,
            IValidator<DownloadRequest> validator,
            GetDownloadLinkHandler handler,
            HttpContext httpContext) =>
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["errors"] = errors });
            }

            var result = await handler.HandleAsync(request);
            if (result is { IsSuccess: true, Value: not null })
            {
                var response = DownloadResponse.WithUrl(
                    result.Value.TempId,
                    httpContext.Request.Scheme,
                    httpContext.Request.Host.Value ?? "localhost");
                return Results.Ok(response);
            }

            return Results.Json(
                new ApiErrorResponse(Error: result.Error!), statusCode: StatusCodes.Status500InternalServerError);
        })
        .WithTags(SwaggerConstants.Tags.Downloads)
        .WithSummary("Request an MP3 download link for a YouTube video")
        .Produces<DownloadResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .RequireAuthorization()
        .RequireRateLimiting(RateLimitPolicyNames.GetDownloadLink);

        return endpoints;
    }
}
