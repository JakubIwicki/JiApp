using FluentValidation;
using JiApp.Common.Abstractions;
using JiApp.Scheduler.Configuration;

namespace JiApp.Scheduler.Features.Clients.CreateClient;

public static class CreateClientEndpoint
{
    public static void MapCreateClient(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/clients", async (
                CreateClientRequest request,
                IValidator<CreateClientRequest> validator,
                CreateClientHandler handler,
                CancellationToken ct) =>
            {
                var validation = await validator.ValidateAsync(request, ct);
                if (!validation.IsValid)
                {
                    var errors = validation.Errors.Select(e => e.ErrorMessage).ToArray();
                    return Results.Extensions.ValidationError(errors);
                }

                var result = await handler.HandleAsync(request, ct);
                return result.IsSuccess
                    ? Results.Created($"/clients/{result.Value}", new { id = result.Value })
                    : result.ErrorCategory switch
                    {
                        ResultCategories.AccessDenied => Results.Forbid(),
                        _ => Results.BadRequest(new ApiErrorResponse(result.Error!))
                    };
            })
            .RequireAuthorization()
            .WithTags(SwaggerConstants.Tags.Clients)
            .WithSummary("Create a client")
            .Produces(StatusCodes.Status201Created);
    }
}