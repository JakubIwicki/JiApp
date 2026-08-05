using JiApp.Common.Abstractions;
using JiApp.Scheduler.Configuration;

namespace JiApp.Scheduler.Features.Expenses.ListExpenses;

public static class ListExpensesEndpoint
{
    public static void MapListExpenses(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/expenses", async (
                long boardId,
                DateOnly? date,
                ListExpensesHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(boardId, date, ct);
                return result.ToHttp();
            })
            .RequireAuthorization()
            .WithTags(SwaggerConstants.Tags.Expenses)
            .WithSummary("List expenses by board (optional date filter)")
            .Produces<List<ExpenseResponse>>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);
    }
}