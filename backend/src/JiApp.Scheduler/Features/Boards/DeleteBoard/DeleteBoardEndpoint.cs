using JiApp.Scheduler.Configuration;
using JiApp.Common.Abstractions;
using JiApp.Common.Middleware;

namespace JiApp.Scheduler.Features.Boards.DeleteBoard;

public static class DeleteBoardEndpoint
{
    public static void MapDeleteBoard(this IEndpointRouteBuilder routes)
    {
        routes.MapDelete("/boards/{id:long}", async (
                long id,
                DeleteBoardHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, ct);
                return result.IsSuccess
                    ? Results.Ok()
                    : result.ToHttp();
            })
            .RequireAuthorization()
            .AddEndpointFilter<SecurityStampRecheckFilter>()
            .WithTags(SwaggerConstants.Tags.Boards)
            .WithSummary("Delete a board")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status400BadRequest);
    }
}