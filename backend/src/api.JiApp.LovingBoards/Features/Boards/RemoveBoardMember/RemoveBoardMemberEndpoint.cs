using JiApp.Common.Abstractions;
using JiApp.Common.Middleware;
using api.JiApp.LovingBoards.Configuration;

namespace api.JiApp.LovingBoards.Features.Boards.RemoveBoardMember;

public static class RemoveBoardMemberEndpoint
{
    public static void MapRemoveBoardMember(this IEndpointRouteBuilder routes)
    {
        routes.MapDelete("/boards/{id:long}/members/{userId:long}", async (
                long id,
                long userId,
                RemoveBoardMemberHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, userId, ct);
                return result.IsSuccess
                    ? Results.Ok()
                    : result.ErrorCategory switch
                    {
                        ResultCategories.NotFound => Results.NotFound(new ApiErrorResponse(result.Error!)),
                        ResultCategories.AccessDenied => Results.Forbid(),
                        ResultCategories.Conflict => Results.Conflict(new ApiErrorResponse(result.Error!)),
                        _ => Results.BadRequest(new ApiErrorResponse(result.Error!))
                    };
            })
            .RequireAuthorization()
            .AddEndpointFilter<SecurityStampRecheckFilter>()
            .WithTags(SwaggerConstants.Tags.Boards)
            .WithSummary("Remove a member from a board")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status400BadRequest);
    }
}
