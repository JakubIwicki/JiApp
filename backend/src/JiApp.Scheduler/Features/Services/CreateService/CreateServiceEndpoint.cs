using JiApp.Scheduler.Configuration;
using FluentValidation;
using JiApp.Common.Abstractions;

namespace JiApp.Scheduler.Features.Services.CreateService;

public static class CreateServiceEndpoint
{
    public static void MapCreateService(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/services", async (
                CreateServiceRequest request,
                IValidator<CreateServiceRequest> validator,
                CreateServiceHandler handler,
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
                    ? Results.Created($"/services/{result.Value}", new { id = result.Value })
                    : result.ErrorCategory switch
                    {
                        ResultCategories.NotFound => Results.NotFound(new ApiErrorResponse(result.Error!)),
                        ResultCategories.AccessDenied => Results.Forbid(),
                        ResultCategories.Validation => Results.BadRequest(new ApiErrorResponse(result.Error!)),
                        _ => Results.Problem(result.Error)
                    };
            })
            .RequireAuthorization()
            .WithTags(SwaggerConstants.Tags.Services)
            .WithSummary("Create a service")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status400BadRequest);
    }
}