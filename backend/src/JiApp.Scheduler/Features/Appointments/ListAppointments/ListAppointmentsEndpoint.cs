using JiApp.Common.Abstractions;
using JiApp.Scheduler.Configuration;

namespace JiApp.Scheduler.Features.Appointments.ListAppointments;

public static class ListAppointmentsEndpoint
{
    public static void MapListAppointments(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/appointments", async (
                long boardId,
                DateOnly[]? dates,
                ListAppointmentsHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(boardId, dates, ct);
                return result.ToHttp();
            })
            .RequireAuthorization()
            .WithTags(SwaggerConstants.Tags.Appointments)
            .WithSummary("List appointments by board (optional date range)")
            .Produces<List<AppointmentResponse>>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);
    }
}