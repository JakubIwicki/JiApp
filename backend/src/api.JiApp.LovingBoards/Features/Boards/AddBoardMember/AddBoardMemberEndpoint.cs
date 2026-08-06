using FluentValidation;
using JiApp.Common.Abstractions;
using JiApp.Common.Middleware;
using api.JiApp.LovingBoards.Configuration;

namespace api.JiApp.LovingBoards.Features.Boards.AddBoardMember;

public static class AddBoardMemberEndpoint
{
    public static void MapAddBoardMember(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/boards/{id:long}/members", async (
                long id,
                AddBoardMemberRequest request,
                IValidator<AddBoardMemberRequest> validator,
                AddBoardMemberHandler handler,
                CancellationToken ct) =>
            {
                var validation = await validator.ValidateAsync(request, ct);
                if (!validation.IsValid)
                {
                    var errors = validation.Errors.Select(e => e.ErrorMessage).ToArray();
                    return Results.Extensions.ValidationError(errors);
                }

                var result = await handler.HandleAsync(id, request, ct);
                return result.IsSuccess
                    ? Results.Created($"/boards/{id}/members", new { id })
                    : result.ToHttp();
            })
            .RequireAuthorization()
            .AddEndpointFilter<SecurityStampRecheckFilter>()
            .WithTags(SwaggerConstants.Tags.Boards)
            .WithSummary("Add a member to a board")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status409Conflict);
    }
}
