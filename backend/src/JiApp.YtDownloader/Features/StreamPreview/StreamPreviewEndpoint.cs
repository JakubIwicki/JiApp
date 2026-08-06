using JiApp.Common.Abstractions;
using JiApp.YtDownloader.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.YtDownloader.Features.StreamPreview;

public static class StreamPreviewEndpoint
{
    public static IEndpointRouteBuilder MapStreamPreview(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/preview/{videoId:regex(^[a-zA-Z0-9_-]{{11}}$)}", async (
                string videoId,
                StreamPreviewHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = handler.Handle(videoId);

                if (!result.IsSuccess)
                    return result.ToHttp();

                var preview = result.Value!;

                // Kill the processes the moment the response finishes — streamed or aborted.
                httpContext.Response.OnCompleted(() => preview.DisposeAsync().AsTask());

                try
                {
                    await preview.StartAsync(cancellationToken);
                }
                catch (Exception)
                {
                    return Results.Json(
                        new ApiErrorResponse(Error: "Failed to start audio stream."),
                        statusCode: StatusCodes.Status502BadGateway);
                }

                return Results.Stream(
                    preview.GetAudioStream(),
                    contentType: "audio/mpeg",
                    enableRangeProcessing: false);
            })
            .WithTags(SwaggerConstants.Tags.Downloads)
            .WithSummary("Stream an audio preview of a YouTube video")
            .Produces(StatusCodes.Status200OK, contentType: "audio/mpeg")
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status502BadGateway)
            .RequireAuthorization();

        return endpoints;
    }
}
