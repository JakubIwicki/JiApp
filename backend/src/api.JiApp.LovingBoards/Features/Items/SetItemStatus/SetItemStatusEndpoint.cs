using FluentValidation;
using JiApp.Common.Abstractions;
using api.JiApp.LovingBoards.Configuration;

namespace api.JiApp.LovingBoards.Features.Items.SetItemStatus;

public static class SetItemStatusEndpoint
{
    public static void MapSetItemStatus(this IEndpointRouteBuilder routes)
    {
        routes.MapPut("/boards/{boardId:long}/items/{itemId:long}/status", async (
                long boardId,
                long itemId,
                SetItemStatusRequest request,
                IValidator<SetItemStatusRequest> validator,
                SetItemStatusHandler handler,
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
            .WithSummary("Set an item's status")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status400BadRequest);
    }
}
