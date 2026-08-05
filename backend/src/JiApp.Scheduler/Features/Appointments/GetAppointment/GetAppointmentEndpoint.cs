using JiApp.Scheduler.Configuration;
using JiApp.Common.Abstractions;

namespace JiApp.Scheduler.Features.Appointments.GetAppointment;

public static class GetAppointmentEndpoint
{
    public static void MapGetAppointment(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/appointments/{id:long}", async (
                long id,
                GetAppointmentHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, ct);
                return result.ToHttp();
            })
            .RequireAuthorization()
            .WithTags(SwaggerConstants.Tags.Appointments)
            .WithSummary("Get an appointment by ID")
            .Produces<AppointmentResponse>()
            .Produces(StatusCodes.Status404NotFound);
    }
}