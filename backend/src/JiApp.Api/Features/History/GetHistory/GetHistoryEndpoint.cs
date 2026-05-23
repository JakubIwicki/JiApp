using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using JiApp.Api.Configuration;
using JiApp.Common.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Api.Features.History.GetHistory;

public static class GetHistoryEndpoint
{
    public static IEndpointRouteBuilder MapGetHistory(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/history", async (
                int? limit,
                IValidator<GetHistoryRequest> validator,
                GetHistoryHandler handler) =>
            {
                var request = new GetHistoryRequest(limit);

                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["errors"] = errors });
                }

                var result = await handler.HandleAsync(request);
                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                return Results.Json(new ApiErrorResponse(Error: result.Error!),
                    statusCode: StatusCodes.Status400BadRequest);
            })
            .WithTags(SwaggerConstants.Tags.History)
            .WithSummary("Get combined search and download history")
            .Produces<GetHistoryResponse>()
            .ProducesValidationProblem()
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicyNames.GetHistory)
            .HasApiVersion(1);

        return endpoints;
    }
}