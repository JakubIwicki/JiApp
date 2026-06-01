using JiApp.Common.Abstractions;
using JiApp.Scheduler.Configuration;

namespace JiApp.Scheduler.Features.Clients.GetClient;

public static class GetClientEndpoint
{
    public static void MapGetClient(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/clients/{id:long}", async (
            long id,
            GetClientHandler handler,
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
        .WithTags(SwaggerConstants.Tags.Clients)
        .WithSummary("Get client details with appointment history")
        .Produces<GetClientResponse>()
        .Produces(StatusCodes.Status404NotFound);
    }
}
