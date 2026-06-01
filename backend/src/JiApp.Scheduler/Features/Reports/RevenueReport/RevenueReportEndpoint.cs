using JiApp.Scheduler.Configuration;
using FluentValidation;
using JiApp.Common.Abstractions;

namespace JiApp.Scheduler.Features.Reports.RevenueReport;

public static class RevenueReportEndpoint
{
    public static void MapRevenueReport(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/reports/revenue", async (
                long boardId,
                DateOnly from,
                DateOnly to,
                string groupBy,
                IValidator<RevenueReportRequest> validator,
                RevenueReportHandler handler,
                CancellationToken ct) =>
            {
                var request = new RevenueReportRequest(boardId, from, to, groupBy);
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
                        ResultCategories.Validation => Results.BadRequest(new ApiErrorResponse(result.Error!)),
                        _ => Results.BadRequest(new ApiErrorResponse(result.Error!))
                    };
            })
            .RequireAuthorization()
            .WithTags(SwaggerConstants.Tags.Reports)
            .WithSummary("Revenue report grouped by weekend, service, location, or client")
            .Produces<List<RevenueReportResponse>>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status400BadRequest);
    }
}