using JiApp.Scheduler.Configuration;
using FluentValidation;
using JiApp.Common.Abstractions;

namespace JiApp.Scheduler.Features.Reports.ClientReport;

public static class ClientReportEndpoint
{
    public static void MapClientReport(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/reports/clients", async (
                long boardId,
                string sortBy,
                IValidator<ClientReportRequest> validator,
                ClientReportHandler handler,
                CancellationToken ct) =>
            {
                var request = new ClientReportRequest(boardId, sortBy);
                var validation = await validator.ValidateAsync(request, ct);
                if (!validation.IsValid)
                {
                    var errors = validation.ErrorMessages();
                    return Results.Extensions.ValidationError(errors);
                }

                var result = await handler.HandleAsync(request, ct);
                return result.ToHttp();
            })
            .RequireAuthorization()
            .WithTags(SwaggerConstants.Tags.Reports)
            .WithSummary("Client report with visit frequency, total spent, and last visit")
            .Produces<List<ClientReportResponse>>();
    }
}