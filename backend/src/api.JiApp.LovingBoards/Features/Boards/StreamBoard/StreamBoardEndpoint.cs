using System.Text.Json;
using JiApp.Common.Abstractions;
using api.JiApp.LovingBoards.Configuration;
using api.JiApp.LovingBoards.Features.Common;
using api.JiApp.LovingBoards.Persistence;
using api.JiApp.LovingBoards.Realtime;

namespace api.JiApp.LovingBoards.Features.Boards.StreamBoard;

public static class StreamBoardEndpoint
{
    private static readonly JsonSerializerOptions SseJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static IEndpointRouteBuilder MapStreamBoard(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/boards/{boardId:long}/stream", async (
                long boardId,
                ILovingBoardsDbContext db,
                ICurrentUserService currentUser,
                IBoardBroadcaster broadcaster,
                HttpContext httpContext) =>
            {
                var boardResult = await BoardAccessGuard.VerifyBoardAccessAsync(db, boardId, currentUser, httpContext.RequestAborted);
                if (!boardResult.IsSuccess)
                    return boardResult.ErrorCategory switch
                    {
                        ResultCategories.NotFound => Results.NotFound(new ApiErrorResponse(boardResult.Error!)),
                        ResultCategories.AccessDenied => Results.Forbid(),
                        _ => Results.BadRequest(new ApiErrorResponse(boardResult.Error!))
                    };

                await StreamSseAsync(httpContext, broadcaster, boardId, currentUser.UserId);

                return Results.Empty;
            })
            .WithTags(SwaggerConstants.Tags.Boards)
            .WithSummary("Stream board events via Server-Sent Events")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);

        return endpoints;
    }

    private static async Task StreamSseAsync(
        HttpContext httpContext,
        IBoardBroadcaster broadcaster,
        long boardId,
        long userId)
    {
        var response = httpContext.Response;
        response.ContentType = "text/event-stream";
        response.Headers.CacheControl = "no-cache";
        response.Headers["X-Accel-Buffering"] = "no";

        httpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>()?.DisableBuffering();

        var ct = httpContext.RequestAborted;

        await response.WriteAsync(": keep-alive\n\n", ct);
        await response.Body.FlushAsync(ct);

        var subscription = broadcaster.Subscribe(boardId, userId);

        try
        {
            using var heartbeat = new PeriodicTimer(TimeSpan.FromSeconds(20));

            var heartbeatTask = heartbeat.WaitForNextTickAsync(ct).AsTask();
            var readTask = ReadNextEventAsync(subscription, ct);

            while (!ct.IsCancellationRequested)
            {
                var completed = await Task.WhenAny(readTask, heartbeatTask);

                if (completed == heartbeatTask)
                {
                    if (!await heartbeatTask)
                        break;

                    await response.WriteAsync(": ping\n\n", ct);
                    await response.Body.FlushAsync(ct);

                    heartbeatTask = heartbeat.WaitForNextTickAsync(ct).AsTask();
                }
                else
                {
                    var ev = await readTask;
                    if (ev is null)
                        break;

                    var json = JsonSerializer.Serialize(ev.Data, SseJsonOptions);
                    await response.WriteAsync($"event: {ev.Event}\n", ct);
                    await response.WriteAsync($"data: {json}\n\n", ct);
                    await response.Body.FlushAsync(ct);

                    readTask = ReadNextEventAsync(subscription, ct);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // client disconnected — stream ends cleanly
        }
        finally
        {
            subscription.Dispose();
        }
    }

    private static async Task<BoardEvent?> ReadNextEventAsync(IBoardSubscription subscription, CancellationToken ct)
    {
        await foreach (var ev in subscription.ReadAllAsync(ct))
            return ev;

        return null;
    }
}
