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
                var result = handler.HandleAsync(videoId, cancellationToken);

                if (result == StreamPreviewResult.ResolveFailed)
                {
                    return Results.NotFound(new ApiErrorResponse(
                        Error: "Could not resolve audio for this video. It may be unavailable or age-restricted."));
                }

                if (result is not StreamPreviewResult.StreamReady stream)
                {
                    return Results.Problem("Unexpected preview state.", statusCode: 500);
                }

                var ytDlp = stream.YtDlpProcess;
                var ffmpeg = stream.FfmpegProcess;

                try
                {
                    ffmpeg.Start();
                    ytDlp.Start();
                }
                catch (Exception)
                {
                    ytDlp.Dispose();
                    ffmpeg.Dispose();
                    return Results.Json(
                        new ApiErrorResponse(Error: "Failed to start audio stream."),
                        statusCode: StatusCodes.Status502BadGateway);
                }

                // Drain stderr on background threads to prevent pipe deadlock
                _ = ytDlp.StandardError.ReadToEndAsync();
                _ = ffmpeg.StandardError.ReadToEndAsync();

                // Pipe yt-dlp stdout into ffmpeg stdin on a background task
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ytDlp.StandardOutput.BaseStream.CopyToAsync(
                            ffmpeg.StandardInput.BaseStream, cancellationToken);
                    }
                    catch (IOException)
                    {
                        // Broken pipe is expected when ffmpeg stops early after -t N
                    }
                    finally
                    {
                        ffmpeg.StandardInput.Close();
                    }
                }, CancellationToken.None);

                var response = ffmpeg.StandardOutput.BaseStream;

                var timeoutSeconds = settings.App!.PreviewDurationSeconds + 2;
                var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, timeoutCts.Token);

                httpContext.Response.OnCompleted(() =>
                {
                    using (timeoutCts)
                    using (linkedCts)
                    {
                        try
                        {
                            if (!ytDlp.HasExited) ytDlp.Kill(entireProcessTree: true);
                        }
                        catch
                        {
                            // Ignore exceptions from killing the process on completion
                        }

                        try
                        {
                            if (!ffmpeg.HasExited) ffmpeg.Kill(entireProcessTree: true);
                        }
                        catch
                        {
                            // Ignore exceptions from killing the process on completion
                        }
                    }

                    ytDlp.Dispose();
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
                    if (!ytDlp.HasExited) ytDlp.Kill(entireProcessTree: true);
                    if (!ffmpeg.HasExited) ffmpeg.Kill(entireProcessTree: true);
                    ytDlp.Dispose();
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
