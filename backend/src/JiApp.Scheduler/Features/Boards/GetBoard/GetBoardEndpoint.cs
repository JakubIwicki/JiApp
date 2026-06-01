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
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ErrorCategory switch
                {
                    ResultCategories.AccessDenied => Results.Forbid(),
                    _ => Results.NotFound(new ApiErrorResponse(result.Error!))
                };
        })
        .RequireAuthorization()
        .WithTags(SwaggerConstants.Tags.Boards)
        .WithSummary("Get board details")
        .Produces<GetBoardResponse>()
        .Produces(StatusCodes.Status404NotFound);
    }
}
