using JiApp.Scheduler.Configuration;
using JiApp.Common.Abstractions;

namespace JiApp.Scheduler.Features.Services.GetService;

public static class GetServiceEndpoint
{
    public static void MapGetService(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/services/{id:long}", async (
                long id,
                GetServiceHandler handler,
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
            .WithTags(SwaggerConstants.Tags.Services)
            .WithSummary("Get a service by ID")
            .Produces<ServiceResponse>()
            .Produces(StatusCodes.Status404NotFound);
    }
}