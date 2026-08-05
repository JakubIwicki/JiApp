using JiApp.Common.Abstractions;
using JiApp.Scheduler.Configuration;

namespace JiApp.Scheduler.Features.Services.ListServices;

public static class ListServicesEndpoint
{
    public static void MapListServices(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/services", async (
                long boardId,
                string? category,
                ListServicesHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(boardId, category, ct);
                return result.ToHttp();
            })
            .RequireAuthorization()
            .WithTags(SwaggerConstants.Tags.Services)
            .WithSummary("List services (optional filters: boardId, category)")
            .Produces<List<ServiceResponse>>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);
    }
}