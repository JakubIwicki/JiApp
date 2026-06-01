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
                Settings settings,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(videoId, cancellationToken);

                if (result == StreamPreviewResult.ResolveFailed)
                {
                    return Results.NotFound(new ApiErrorResponse(
                        Error: "Could not resolve audio for this video. It may be unavailable or age-restricted."));
                }

                if (result is not StreamPreviewResult.StreamReady stream)
                {
                    return Results.Problem("Unexpected preview state.", statusCode: 500);
                }

                var ffmpeg = stream.FfmpegProcess;

                try
                {
                    ffmpeg.Start();
                    // Consume stderr on a background thread to prevent pipe deadlock
                    _ = ffmpeg.StandardError.ReadToEndAsync();
                }
                catch (Exception)
                {
                    ffmpeg.Dispose();
                    return Results.Json(
                        new ApiErrorResponse(Error: "Failed to start audio stream."),
                        statusCode: StatusCodes.Status502BadGateway);
                }

                var response = ffmpeg.StandardOutput.BaseStream;

                var timeoutSeconds = settings.App!.PreviewDurationSeconds + 2;
                var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, timeoutCts.Token);

                // Single cleanup path: OnCompleted fires when the response completes,
                // regardless of whether it completed normally or via client disconnect.
                // The linked CTS handles timeout and cancellation — the ffmpeg process
                // is killed once in OnCompleted, avoiding a race with Register.
                httpContext.Response.OnCompleted(() =>
                {
                    using (timeoutCts)
                    using (linkedCts)
                    {
                        try
                        {
                            if (!ffmpeg.HasExited) ffmpeg.Kill(entireProcessTree: true);
                        }
                        catch
                        {
                            // Ignore exceptions from killing the process on completion
                        }
                    }

                    ffmpeg.Dispose();
                    return Task.CompletedTask;
                });

                try
                {
                    return Results.Stream(
                        response,
                        contentType: "audio/mpeg",
                        enableRangeProcessing: false);
                }
                catch
                {
                    // If returning the result fails (e.g. cancellation before response starts),
                    // ensure ffmpeg is cleaned up. OnCompleted may not fire in this edge case.
                    if (!ffmpeg.HasExited) ffmpeg.Kill(entireProcessTree: true);
                    ffmpeg.Dispose();
                    throw;
                }
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