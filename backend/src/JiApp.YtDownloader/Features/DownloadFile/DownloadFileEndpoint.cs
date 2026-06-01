using JiApp.Common.Abstractions;
using JiApp.YtDownloader.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.YtDownloader.Features.DownloadFile;

public static class DownloadFileEndpoint
{
    public static IEndpointRouteBuilder MapDownloadFile(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/downloads/mp3/file/{id}", (string id, DownloadFileHandler handler) =>
            {
                var result = handler.Handle(id);
                if (result.IsSuccess)
                    return Results.File(result.Value!, "audio/mpeg");

                return Results.Json(new ApiErrorResponse(Error: result.Error ?? ApiErrorResponse.UnknownErrorMessage),
                    statusCode: StatusCodes.Status404NotFound);
            })
            .WithTags(SwaggerConstants.Tags.Downloads)
            .WithSummary("Download the MP3 file by temporary ID")
            .Produces(StatusCodes.Status200OK, contentType: "audio/mpeg")
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        return endpoints;
    }
}
