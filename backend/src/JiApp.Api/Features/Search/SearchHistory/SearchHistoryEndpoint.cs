using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using JiApp.Api.Configuration;
using JiApp.Common.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Api.Features.Search.SearchHistory;

public static class SearchHistoryEndpoint
{
    public static IEndpointRouteBuilder MapSearchHistory(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/search/history", async (
                int? limit,
                IValidator<SearchHistoryRequest> validator,
                SearchHistoryHandler handler) =>
            {
                var request = new SearchHistoryRequest(limit);

                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["errors"] = errors });
                }

                var result = await handler.HandleAsync(request);
                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                return Results.Json(new ApiErrorResponse(Error: result.Error ?? "An unknown error occurred"),
                    statusCode: StatusCodes.Status400BadRequest);
            })
            .WithTags(SwaggerConstants.Tags.Search)
            .WithSummary("Get search history for authenticated user")
            .Produces<SearchHistoryResponse>()
            .ProducesValidationProblem()
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicyNames.SearchHistory)
            .HasApiVersion(1);

        return endpoints;
    }
}