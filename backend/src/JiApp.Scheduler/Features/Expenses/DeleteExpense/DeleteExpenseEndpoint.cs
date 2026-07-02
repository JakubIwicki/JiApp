using JiApp.Scheduler.Configuration;
using JiApp.Common.Abstractions;
using JiApp.Common.Middleware;

namespace JiApp.Scheduler.Features.Expenses.DeleteExpense;

public static class DeleteExpenseEndpoint
{
    public static void MapDeleteExpense(this IEndpointRouteBuilder routes)
    {
        routes.MapDelete("/expenses/{id:long}", async (
                long id,
                DeleteExpenseHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, ct);
                return result.IsSuccess
                    ? Results.Ok(new { id = result.Value })
                    : Results.NotFound(new ApiErrorResponse(result.Error!));
            })
            .RequireAuthorization()
            .AddEndpointFilter<SecurityStampRecheckFilter>()
            .WithTags(SwaggerConstants.Tags.Expenses)
            .WithSummary("Delete an expense")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}