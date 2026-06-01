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
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.ErrorCategory switch
                    {
                        ResultCategories.AccessDenied => Results.Forbid(),
                        _ => Results.NotFound(new ApiErrorResponse(result.Error!))
                    };
            })
            .RequireAuthorization()
            .WithTags(SwaggerConstants.Tags.Appointments)
            .WithSummary("Get an appointment by ID")
            .Produces<AppointmentResponse>()
            .Produces(StatusCodes.Status404NotFound);
    }
}