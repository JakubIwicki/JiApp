using FluentValidation;
using JiApp.Common.Abstractions;

namespace JiApp.Api.Features.History.GetHistory;

public static class GetHistoryEndpoint
{
    public static IEndpointRouteBuilder MapGetHistory(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/history", async (
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

            return Results.Json(new ApiErrorResponse(Error: result.Error!), statusCode: StatusCodes.Status400BadRequest);
        })
        .WithTags("History")
        .WithSummary("Get combined search and download history")
        .Produces<GetHistoryResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .RequireAuthorization();

        return endpoints;
    }
}
