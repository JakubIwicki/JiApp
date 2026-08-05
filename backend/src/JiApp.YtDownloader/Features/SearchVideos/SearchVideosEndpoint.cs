using FluentValidation;
using JiApp.Common.Abstractions;
using JiApp.YtDownloader.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.YtDownloader.Features.SearchVideos;

public static class SearchVideosEndpoint
{
    public static IEndpointRouteBuilder MapSearchVideos(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/search", async (
                SearchVideosRequest request,
                IValidator<SearchVideosRequest> validator,
                SearchVideosHandler handler,
                HttpContext httpContext,
                CancellationToken ct) =>
            {
                var validationResult = await validator.ValidateAsync(request, ct);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
                    return Results.Extensions.ValidationError(errors);
                }

                var result = await handler.HandleAsync(request, httpContext.RequestAborted);
                return result.ToHttp();
            })
            .WithTags(SwaggerConstants.Tags.Search)
            .WithSummary("Search YouTube videos by query")
            .Produces<SearchVideosResponse>()
            .ProducesValidationProblem()
            .Produces<ApiErrorResponse>(StatusCodes.Status502BadGateway)
            .RequireAuthorization();

        return endpoints;
    }
}
