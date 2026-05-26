using System;
using System.Threading;
using System.Threading.Tasks;
using JiApp.Api.Configuration;
using JiApp.Common.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Api.Features.Preview.StreamPreview;

public static class StreamPreviewEndpoint
{
    public static IEndpointRouteBuilder MapStreamPreview(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/preview/{videoId}", async (
                string videoId,
                StreamPreviewHandler handler,
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
                }
                catch (Exception)
                {
                    ffmpeg.Dispose();
                    return Results.Json(
                        new ApiErrorResponse(Error: "Failed to start audio stream."),
                        statusCode: StatusCodes.Status502BadGateway);
                }

                var response = ffmpeg.StandardOutput.BaseStream;

                // Kill ffmpeg when client disconnects or after ~12s (10s clip + buffer)
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(12));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, timeoutCts.Token);

                linkedCts.Token.Register(() =>
                {
                    try
                    {
                        if (!ffmpeg.HasExited) ffmpeg.Kill(entireProcessTree: true);
                    }
                    catch
                    {
                        /* process may already be gone */
                    }

                    ffmpeg.Dispose();
                });

                return Results.Stream(
                    response,
                    contentType: "audio/mpeg",
                    enableRangeProcessing: false);
            })
            .WithTags(SwaggerConstants.Tags.Downloads)
            .WithSummary("Stream a 10-second audio preview of a YouTube video")
            .Produces(StatusCodes.Status200OK, contentType: "audio/mpeg")
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status502BadGateway)
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicyNames.Preview)
            .HasApiVersion(1);

        return endpoints;
    }
}