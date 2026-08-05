using JiApp.Common.Abstractions;
using JiApp.Common.Middleware;
using JiApp.Scheduler.Configuration;

namespace JiApp.Scheduler.Features.Clients.DeleteClient;

public static class DeleteClientEndpoint
{
    public static void MapDeleteClient(this IEndpointRouteBuilder routes)
    {
        routes.MapDelete("/clients/{id:long}", async (
                long id,
                DeleteClientHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, ct);
                return result.IsSuccess
                    ? Results.Ok()
                    : result.ToHttp();
            })
            .RequireAuthorization()
            .AddEndpointFilter<SecurityStampRecheckFilter>()
            .WithTags(SwaggerConstants.Tags.Clients)
            .WithSummary("Delete a client (fails if client has appointments)")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);
    }
}