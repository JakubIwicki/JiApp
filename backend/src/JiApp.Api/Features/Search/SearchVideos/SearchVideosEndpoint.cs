using FluentValidation;
using JiApp.Common.Abstractions;

namespace JiApp.Api.Features.Search.SearchVideos;

public static class SearchVideosEndpoint
{
    public static IEndpointRouteBuilder MapSearchVideos(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/search", async (
            SearchVideosRequest request,
            IValidator<SearchVideosRequest> validator,
            SearchVideosHandler handler) =>
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["errors"] = errors });
            }

            var result = await handler.HandleAsync(request);
            if (result.IsSuccess)
                return Results.Ok(result.Value);

            return Results.Json(
                new ApiErrorResponse(Error: result.Error!), statusCode: StatusCodes.Status502BadGateway);
        })
        .WithTags("Search")
        .WithSummary("Search YouTube videos by query")
        .Produces<SearchVideosResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status502BadGateway)
        .RequireAuthorization();

        return endpoints;
    }
}
