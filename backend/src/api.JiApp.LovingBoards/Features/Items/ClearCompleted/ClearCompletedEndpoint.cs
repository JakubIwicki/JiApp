using JiApp.Common.Abstractions;
using api.JiApp.LovingBoards.Configuration;

namespace api.JiApp.LovingBoards.Features.Items.ClearCompleted;

public static class ClearCompletedEndpoint
{
    public static void MapClearCompleted(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/boards/{boardId:long}/items/clear-completed", async (
                long boardId,
                ClearCompletedHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(boardId, ct);
                return result.IsSuccess
                    ? Results.Ok(new { cleared = result.Value })
                    : result.ErrorCategory switch
                    {
                        ResultCategories.NotFound => Results.NotFound(new ApiErrorResponse(result.Error!)),
                        ResultCategories.AccessDenied => Results.Forbid(),
                        _ => Results.BadRequest(new ApiErrorResponse(result.Error!))
                    };
            })
            .RequireAuthorization()
            .WithTags(SwaggerConstants.Tags.Items)
            .WithSummary("Clear all completed items on a board (soft-remove)")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status400BadRequest);
    }
}
