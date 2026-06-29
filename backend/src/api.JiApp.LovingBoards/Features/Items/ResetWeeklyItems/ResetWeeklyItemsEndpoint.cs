using JiApp.Common.Abstractions;
using api.JiApp.LovingBoards.Configuration;

namespace api.JiApp.LovingBoards.Features.Items.ResetWeeklyItems;

public static class ResetWeeklyItemsEndpoint
{
    public static void MapResetWeeklyItems(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/boards/{boardId:long}/items/reset-weekly", async (
                long boardId,
                ResetWeeklyItemsHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(boardId, ct);
                return result.IsSuccess
                    ? Results.Ok(new { reset = result.Value })
                    : result.ErrorCategory switch
                    {
                        ResultCategories.NotFound => Results.NotFound(new ApiErrorResponse(result.Error!)),
                        ResultCategories.AccessDenied => Results.Forbid(),
                        _ => Results.BadRequest(new ApiErrorResponse(result.Error!))
                    };
            })
            .RequireAuthorization()
            .WithTags(SwaggerConstants.Tags.Items)
            .WithSummary("Force reset all recurring items to needed")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status400BadRequest);
    }
}
