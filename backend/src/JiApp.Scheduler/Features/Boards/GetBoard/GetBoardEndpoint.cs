using JiApp.Common.Abstractions;
using JiApp.Scheduler.Configuration;

namespace JiApp.Scheduler.Features.Boards.GetBoard;

public static class GetBoardEndpoint
{
    public static void MapGetBoard(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/boards/{id:long}", async (
            long id,
            GetBoardHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result.ToHttp();
        })
        .RequireAuthorization()
        .WithTags(SwaggerConstants.Tags.Boards)
        .WithSummary("Get board details")
        .Produces<GetBoardResponse>()
        .Produces(StatusCodes.Status404NotFound);
    }
}
