using JiApp.Scheduler.Configuration;
using FluentValidation;
using JiApp.Common.Abstractions;

namespace JiApp.Scheduler.Features.Appointments.CreateAppointment;

public static class CreateAppointmentEndpoint
{
    public static void MapCreateAppointment(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/appointments", async (
                CreateAppointmentRequest request,
                IValidator<CreateAppointmentRequest> validator,
                CreateAppointmentHandler handler,
                CancellationToken ct) =>
            {
                var validation = await validator.ValidateAsync(request, ct);
                if (!validation.IsValid)
                {
                    var errors = validation.ErrorMessages();
                    return Results.Extensions.ValidationError(errors);
                }

                var result = await handler.HandleAsync(request, ct);
                return result.IsSuccess
                    ? Results.Created($"/appointments/{result.Value}", new { id = result.Value })
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
            .WithSummary("Create an appointment")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status400BadRequest);
    }
}