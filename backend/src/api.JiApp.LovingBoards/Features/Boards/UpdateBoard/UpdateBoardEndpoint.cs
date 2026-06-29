using FluentValidation;
using JiApp.Common.Abstractions;
using api.JiApp.LovingBoards.Configuration;

namespace api.JiApp.LovingBoards.Features.Boards.UpdateBoard;

public static class UpdateBoardEndpoint
{
    public static void MapUpdateBoard(this IEndpointRouteBuilder routes)
    {
        routes.MapPut("/boards/{id:long}", async (
            long id,
            UpdateBoardRequest request,
            IValidator<UpdateBoardRequest> validator,
            UpdateBoardHandler handler,
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
                ? Results.Ok()
                : result.ErrorCategory switch
                {
                    ResultCategories.AccessDenied => Results.Forbid(),
                    _ => Results.NotFound(new ApiErrorResponse(result.Error!))
                };
        })
        .RequireAuthorization()
        .WithTags(SwaggerConstants.Tags.Boards)
        .WithSummary("Update board name")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }
}
