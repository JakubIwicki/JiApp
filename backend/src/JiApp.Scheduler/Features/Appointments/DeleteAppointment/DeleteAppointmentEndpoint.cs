using JiApp.Scheduler.Configuration;
using JiApp.Common.Abstractions;

namespace JiApp.Scheduler.Features.Appointments.DeleteAppointment;

public static class DeleteAppointmentEndpoint
{
    public static void MapDeleteAppointment(this IEndpointRouteBuilder routes)
    {
        routes.MapDelete("/appointments/{id:long}", async (
                long id,
                DeleteAppointmentHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, ct);
                return result.IsSuccess
                    ? Results.Ok()
                    : result.ErrorCategory switch
                    {
                        ResultCategories.NotFound => Results.NotFound(new ApiErrorResponse(result.Error!)),
                        ResultCategories.AccessDenied => Results.Forbid(),
                        _ => Results.Conflict(new ApiErrorResponse(result.Error!))
                    };
            })
            .RequireAuthorization()
            .WithTags(SwaggerConstants.Tags.Appointments)
            .WithSummary("Delete an appointment")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status409Conflict);
    }
}