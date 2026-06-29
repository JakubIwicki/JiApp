using FluentValidation;
using JiApp.Common.Abstractions;
using api.JiApp.LovingBoards.Configuration;

namespace api.JiApp.LovingBoards.Features.Items.UpdateItem;

public static class UpdateItemEndpoint
{
    public static void MapUpdateItem(this IEndpointRouteBuilder routes)
    {
        routes.MapPut("/boards/{boardId:long}/items/{itemId:long}", async (
                long boardId,
                long itemId,
                UpdateItemRequest request,
                IValidator<UpdateItemRequest> validator,
                UpdateItemHandler handler,
                CancellationToken ct) =>
            {
                var validation = await validator.ValidateAsync(request, ct);
                if (!validation.IsValid)
                {
                    var errors = validation.Errors.Select(e => e.ErrorMessage).ToArray();
                    return Results.Extensions.ValidationError(errors);
                }

                var result = await handler.HandleAsync(boardId, itemId, request, ct);
                return result.IsSuccess
                    ? Results.Ok(new { id = result.Value })
                    : result.ErrorCategory switch
                    {
                        ResultCategories.AccessDenied => Results.Forbid(),
                        ResultCategories.NotFound => Results.NotFound(new ApiErrorResponse(result.Error!)),
                        _ => Results.BadRequest(new ApiErrorResponse(result.Error!))
                    };
            })
            .RequireAuthorization()
            .WithTags(SwaggerConstants.Tags.Items)
            .WithSummary("Update an item on a board")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status400BadRequest);
    }
}
