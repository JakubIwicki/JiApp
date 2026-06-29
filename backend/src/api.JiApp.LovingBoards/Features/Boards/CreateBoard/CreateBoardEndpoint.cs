using FluentValidation;
using JiApp.Common.Abstractions;
using api.JiApp.LovingBoards.Configuration;

namespace api.JiApp.LovingBoards.Features.Boards.CreateBoard;

public static class CreateBoardEndpoint
{
    public static void MapCreateBoard(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/boards", async (
                CreateBoardRequest request,
                IValidator<CreateBoardRequest> validator,
                CreateBoardHandler handler,
                CancellationToken ct) =>
            {
                var validation = await validator.ValidateAsync(request, ct);
                if (!validation.IsValid)
                {
                    var errors = validation.Errors.Select(e => e.ErrorMessage).ToArray();
                    return Results.Extensions.ValidationError(errors);
                }

                var result = await handler.HandleAsync(request, ct);
                return result.IsSuccess
                    ? Results.Created($"/boards/{result.Value}", new { id = result.Value })
                    : Results.BadRequest(new ApiErrorResponse(result.Error!));
            })
            .RequireAuthorization()
            .WithTags(SwaggerConstants.Tags.Boards)
            .WithSummary("Create a board")
            .Produces(StatusCodes.Status201Created);
    }
}
