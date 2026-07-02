using JiApp.Scheduler.Configuration;
using JiApp.Common.Abstractions;
using JiApp.Common.Middleware;

namespace JiApp.Scheduler.Features.Services.DeleteService;

public static class DeleteServiceEndpoint
{
    public static void MapDeleteService(this IEndpointRouteBuilder routes)
    {
        routes.MapDelete("/services/{id:long}", async (
                long id,
                DeleteServiceHandler handler,
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
            .AddEndpointFilter<SecurityStampRecheckFilter>()
            .WithTags(SwaggerConstants.Tags.Services)
            .WithSummary("Delete a service (fails if service is used in appointments)")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);
    }
}