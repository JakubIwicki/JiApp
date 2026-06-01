using JiApp.Scheduler.Configuration;
using JiApp.Common.Abstractions;
using FluentValidation;

namespace JiApp.Scheduler.Features.Appointments.UpdateAppointmentStatus;

public static class UpdateAppointmentStatusEndpoint
{
    public static void MapUpdateAppointmentStatus(this IEndpointRouteBuilder routes)
    {
        routes.MapPatch("/appointments/{id:long}/status", async (
                long id,
                UpdateAppointmentStatusRequest request,
                IValidator<UpdateAppointmentStatusRequest> validator,
                UpdateAppointmentStatusHandler handler,
                CancellationToken ct) =>
            {
                var validation = await validator.ValidateAsync(request, ct);
                if (!validation.IsValid)
                {
                    var errors = validation.ErrorMessages();
                    return Results.Extensions.ValidationError(errors);
                }

                var result = await handler.HandleAsync(id, request, ct);
                return result.IsSuccess
                    ? Results.Ok(new { id = result.Value })
                    : result.ErrorCategory switch
                    {
                        ResultCategories.NotFound => Results.NotFound(new ApiErrorResponse(result.Error!)),
                        ResultCategories.AccessDenied => Results.Forbid(),
                        ResultCategories.Validation => Results.BadRequest(new ApiErrorResponse(result.Error!)),
                        _ => Results.Conflict(new ApiErrorResponse(result.Error!))
                    };
            })
            .RequireAuthorization()
            .WithTags(SwaggerConstants.Tags.Appointments)
            .WithSummary("Update appointment status (done or cancel)")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status400BadRequest);
    }
}