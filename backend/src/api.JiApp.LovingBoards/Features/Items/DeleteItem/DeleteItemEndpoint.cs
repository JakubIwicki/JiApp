using JiApp.Common.Abstractions;
using JiApp.Common.Middleware;
using api.JiApp.LovingBoards.Configuration;

namespace api.JiApp.LovingBoards.Features.Items.DeleteItem;

public static class DeleteItemEndpoint
{
    public static void MapDeleteItem(this IEndpointRouteBuilder routes)
    {
        routes.MapDelete("/boards/{boardId:long}/items/{itemId:long}", async (
                long boardId,
                long itemId,
                DeleteItemHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(boardId, itemId, ct);
                return result.IsSuccess
                    ? Results.Ok()
                    : result.ErrorCategory switch
                    {
                        ResultCategories.NotFound => Results.NotFound(new ApiErrorResponse(result.Error!)),
                        ResultCategories.AccessDenied => Results.Forbid(),
                        _ => Results.BadRequest(new ApiErrorResponse(result.Error!))
                    };
            })
            .RequireAuthorization()
            .AddEndpointFilter<SecurityStampRecheckFilter>()
            .WithTags(SwaggerConstants.Tags.Items)
            .WithSummary("Delete an item")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status400BadRequest);
    }
}
