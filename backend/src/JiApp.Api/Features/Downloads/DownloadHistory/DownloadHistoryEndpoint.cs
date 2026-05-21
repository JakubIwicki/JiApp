using FluentValidation;
using JiApp.Common.Abstractions;

namespace JiApp.Api.Features.Downloads.DownloadHistory;

public static class DownloadHistoryEndpoint
{
    public static IEndpointRouteBuilder MapDownloadHistory(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/downloads/history", async (
            int? limit,
            IValidator<DownloadHistoryRequest> validator,
            DownloadHistoryHandler handler) =>
        {
            var request = new DownloadHistoryRequest(limit);

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
        .WithTags("Downloads")
        .WithSummary("Get download history for authenticated user")
        .Produces<DownloadHistoryResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .RequireAuthorization();

        return endpoints;
    }
}
