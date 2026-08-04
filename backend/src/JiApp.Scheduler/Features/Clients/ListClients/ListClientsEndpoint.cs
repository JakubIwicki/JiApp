using JiApp.Common.Abstractions;
using JiApp.Scheduler.Configuration;

namespace JiApp.Scheduler.Features.Clients.ListClients;

public static class ListClientsEndpoint
{
    public static void MapListClients(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/clients", async (
                string? q,
                int? skip,
                int? take,
                SchedulerSettings settings,
                ListClientsHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(q, skip ?? 0, settings.ClampTake(take), ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.BadRequest(new ApiErrorResponse(result.Error!));
            })
            .RequireAuthorization()
            .WithTags(SwaggerConstants.Tags.Clients)
            .WithSummary("List clients (optional search by name)")
            .Produces<List<ClientResponse>>();
    }
}