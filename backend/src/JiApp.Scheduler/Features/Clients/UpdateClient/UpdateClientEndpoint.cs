using FluentValidation;
using JiApp.Common.Abstractions;
using JiApp.Scheduler.Configuration;

namespace JiApp.Scheduler.Features.Clients.UpdateClient;

public static class UpdateClientEndpoint
{
    public static void MapUpdateClient(this IEndpointRouteBuilder routes)
    {
        routes.MapPut("/clients/{id:long}", async (
            long id,
            UpdateClientRequest request,
            IValidator<UpdateClientRequest> validator,
            UpdateClientHandler handler,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToArray();
                return Results.Extensions.ValidationError(errors);
            }

            var result = await handler.HandleAsync(id, request, ct);
            return result.IsSuccess
                ? Results.Ok()
                : result.ErrorCategory switch
                {
                    ResultCategories.AccessDenied => Results.Forbid(),
                    _ => Results.NotFound(new ApiErrorResponse(result.Error!))
                };
        })
        .RequireAuthorization()
        .WithTags(SwaggerConstants.Tags.Clients)
        .WithSummary("Update a client")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }
}
