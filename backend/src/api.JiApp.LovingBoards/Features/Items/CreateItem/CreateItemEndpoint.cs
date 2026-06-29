using FluentValidation;
using JiApp.Common.Abstractions;
using api.JiApp.LovingBoards.Configuration;

namespace api.JiApp.LovingBoards.Features.Items.CreateItem;

public static class CreateItemEndpoint
{
    public static void MapCreateItem(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/boards/{boardId:long}/items", async (
                long boardId,
                CreateItemRequest request,
                IValidator<CreateItemRequest> validator,
                CreateItemHandler handler,
                CancellationToken ct) =>
            {
                var validation = await validator.ValidateAsync(request, ct);
                if (!validation.IsValid)
                {
                    var errors = validation.Errors.Select(e => e.ErrorMessage).ToArray();
                    return Results.Extensions.ValidationError(errors);
                }

                var result = await handler.HandleAsync(boardId, request, ct);
                return result.IsSuccess
                    ? Results.Created($"/boards/{boardId}/items/{result.Value}", new { id = result.Value })
                    : result.ErrorCategory switch
                    {
                        ResultCategories.AccessDenied => Results.Forbid(),
                        ResultCategories.NotFound => Results.NotFound(new ApiErrorResponse(result.Error!)),
                        _ => Results.BadRequest(new ApiErrorResponse(result.Error!))
                    };
            })
            .RequireAuthorization()
            .WithTags(SwaggerConstants.Tags.Items)
            .WithSummary("Create an item on a board")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status400BadRequest);
    }
}
