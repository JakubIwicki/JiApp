using JiApp.Scheduler.Configuration;
using JiApp.Common.Abstractions;

namespace JiApp.Scheduler.Features.Expenses.GetExpense;

public static class GetExpenseEndpoint
{
    public static void MapGetExpense(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/expenses/{id:long}", async (
                long id,
                GetExpenseHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.NotFound(new ApiErrorResponse(result.Error!));
            })
            .RequireAuthorization()
            .WithTags(SwaggerConstants.Tags.Expenses)
            .WithSummary("Get an expense by ID")
            .Produces<ExpenseResponse>()
            .Produces(StatusCodes.Status404NotFound);
    }
}