using FluentValidation;
using JiApp.Common.Abstractions;
using JiApp.Scheduler.Configuration;

namespace JiApp.Scheduler.Features.DayTotals;

public static class DayTotalsEndpoint
{
    public static void MapDayTotals(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/day-totals", async (
                long boardId,
                DateOnly date,
                IValidator<DayTotalsRequest> validator,
                DayTotalsHandler handler,
                CancellationToken ct) =>
            {
                var request = new DayTotalsRequest(boardId, date);
                var validation = await validator.ValidateAsync(request, ct);
                if (!validation.IsValid)
                {
                    var errors = validation.ErrorMessages();
                    return Results.Extensions.ValidationError(errors);
                }

                var result = await handler.HandleAsync(request, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.ErrorCategory switch
                    {
                        ResultCategories.NotFound => Results.NotFound(new ApiErrorResponse(result.Error!)),
                        ResultCategories.AccessDenied => Results.Forbid(),
                        _ => Results.Problem(result.Error)
                    };
            })
            .RequireAuthorization()
            .WithTags(SwaggerConstants.Tags.DayTotals)
            .WithSummary("Get daily P&L with revenue, expenses, and net")
            .Produces<DayTotalsResponse>();
    }
}