using System.Linq;
using FluentValidation;
using JiApp.Common.Abstractions;
using JiApp.YtDownloader.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.YtDownloader.Features.GetDownloadLink;

public static class GetDownloadLinkEndpoint
{
    private const int MaxTitleLength = 300;
    private const int MaxDescriptionLength = 1000;
    private const int MaxImageUrlLength = 300;

    public static IEndpointRouteBuilder MapGetDownloadLink(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/downloads/mp3", async (
                DownloadRequest request,
                IValidator<DownloadRequest> validator,
                GetDownloadLinkHandler handler,
                HttpContext httpContext) =>
            {
                request = TruncateMetadata(request);

                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
                    return Results.Extensions.ValidationError(errors);
                }

                var result = await handler.HandleAsync(request);
                var scheme = httpContext.Request.Headers["X-Forwarded-Proto"].FirstOrDefault()
                             ?? httpContext.Request.Scheme;
                var host = httpContext.Request.Headers["X-Forwarded-Host"].FirstOrDefault()
                           ?? httpContext.Request.Host.Value
                           ?? "localhost";
                var response = DownloadResponse.WithUrl(
                    result.Value!.TempId,
                    scheme,
                    host);
                return Results.Ok(response);
            })
            .WithTags(SwaggerConstants.Tags.Downloads)
            .WithSummary("Request an MP3 download link for a YouTube video")
            .Produces<DownloadResponse>()
            .ProducesValidationProblem()
            .RequireAuthorization();

        return endpoints;
    }

    private static DownloadRequest TruncateMetadata(DownloadRequest request) => request with
    {
        Title = Truncate(request.Title, MaxTitleLength),
        Description = Truncate(request.Description, MaxDescriptionLength),
        ImageUrl = Truncate(request.ImageUrl, MaxImageUrlLength)
    };

    private static string? Truncate(string? value, int maxLength)
    {
        if (value is null || value.Length <= maxLength)
            return value;

        // Never split a UTF-16 surrogate pair (emoji titles).
        return char.IsLowSurrogate(value[maxLength])
            ? value[..(maxLength - 1)]
            : value[..maxLength];
    }
}
