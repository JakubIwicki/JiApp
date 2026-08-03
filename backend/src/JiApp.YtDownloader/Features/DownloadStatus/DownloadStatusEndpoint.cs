using JiApp.Common.Abstractions;
using JiApp.YtDownloader.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.YtDownloader.Features.DownloadStatus;

public static class DownloadStatusEndpoint
{
    public static IEndpointRouteBuilder MapDownloadStatus(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/downloads/mp3/status/{id}", (string id, DownloadStatusHandler handler) =>
            {
                var result = handler.Handle(id);
                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                return Results.Json(new ApiErrorResponse(Error: result.Error ?? ApiErrorResponse.UnknownErrorMessage),
                    statusCode: StatusCodes.Status404NotFound);
            })
            .WithTags(SwaggerConstants.Tags.Downloads)
            .WithSummary("Get the status of an MP3 download job")
            .Produces<DownloadStatusResponse>()
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        return endpoints;
    }
}
