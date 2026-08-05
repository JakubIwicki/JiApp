using FluentValidation;
using JiApp.Common.Abstractions;
using JiApp.YtDownloader.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.YtDownloader.Features.DownloadHistory;

public static class DownloadHistoryEndpoint
{
    public static IEndpointRouteBuilder MapDownloadHistory(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/downloads/history", async (
                int? limit,
                IValidator<DownloadHistoryRequest> validator,
                DownloadHistoryHandler handler) =>
            {
                var request = new DownloadHistoryRequest(limit);

                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
                    return Results.Extensions.ValidationError(errors);
                }

                var result = await handler.HandleAsync(request);
                return result.ToHttp();
            })
            .WithTags(SwaggerConstants.Tags.Downloads)
            .WithSummary("Get download history for authenticated user")
            .Produces<DownloadHistoryResponse>()
            .ProducesValidationProblem()
            .RequireAuthorization();

        return endpoints;
    }
}