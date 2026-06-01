using JiApp.Scheduler.Configuration;
using FluentValidation;
using JiApp.Common.Abstractions;

namespace JiApp.Scheduler.Features.Appointments.UpdateAppointment;

public static class UpdateAppointmentEndpoint
{
    public static void MapUpdateAppointment(this IEndpointRouteBuilder routes)
    {
        routes.MapPut("/appointments/{id:long}", async (
                long id,
                UpdateAppointmentRequest request,
                IValidator<UpdateAppointmentRequest> validator,
                UpdateAppointmentHandler handler,
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
            .WithSummary("Update an appointment")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status400BadRequest);
    }
}