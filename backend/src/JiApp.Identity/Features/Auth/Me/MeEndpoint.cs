using JiApp.Common.Abstractions;
using JiApp.Identity.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Identity.Features.Auth.Me;

public static class MeEndpoint
{
    public static IEndpointRouteBuilder MapMe(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/me", async (
                MeHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(ct);
                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                return Results.Json(
                    new ApiErrorResponse(Error: "Token invalid or expired"),
                    statusCode: StatusCodes.Status401Unauthorized);
            })
            .WithTags(SwaggerConstants.Tags.Auth)
            .WithSummary("Get current authenticated user")
            .Produces<MeResponse>()
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        return endpoints;
    }
}