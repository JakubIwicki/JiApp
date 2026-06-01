using FluentValidation;
using JiApp.Common.Abstractions;
using JiApp.YtDownloader.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.YtDownloader.Features.GetDownloadLink;

public static class GetDownloadLinkEndpoint
{
    public static IEndpointRouteBuilder MapGetDownloadLink(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/downloads/mp3", async (
                DownloadRequest request,
                IValidator<DownloadRequest> validator,
                GetDownloadLinkHandler handler,
                HttpContext httpContext) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
                    return Results.Extensions.ValidationError(errors);
                }

                var result = await handler.HandleAsync(request, httpContext.RequestAborted);
                if (result is { IsSuccess: true, Value: not null })
                {
                    var response = DownloadResponse.WithUrl(
                        result.Value.TempId,
                        httpContext.Request.Scheme,
                        httpContext.Request.Host.Value ?? "localhost");
                    return Results.Ok(response);
                }

                var statusCode = result.ErrorCategory == GetDownloadLinkHandler.YoutubeDlErrorCategory
                    ? StatusCodes.Status502BadGateway
                    : StatusCodes.Status500InternalServerError;

                return Results.Json(
                    new ApiErrorResponse(Error: result.Error ?? ApiErrorResponse.UnknownErrorMessage), statusCode: statusCode);
            })
            .WithTags(SwaggerConstants.Tags.Downloads)
            .WithSummary("Request an MP3 download link for a YouTube video")
            .Produces<DownloadResponse>()
            .ProducesValidationProblem()
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .Produces<ApiErrorResponse>(StatusCodes.Status502BadGateway)
            .RequireAuthorization();

        return endpoints;
    }
}