using JiApp.Common.Abstractions;
using JiApp.Scheduler.Configuration;

namespace JiApp.Scheduler.Features.Boards.ListBoards;

public static class ListBoardsEndpoint
{
    public static void MapListBoards(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/boards", async (
                ListBoardsHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.BadRequest(new ApiErrorResponse(result.Error!));
            })
            .RequireAuthorization()
            .WithTags(SwaggerConstants.Tags.Boards)
            .WithSummary("List all boards for the current user")
            .Produces<ListBoardsResponse>();
    }
}