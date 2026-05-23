using JiApp.Api.Configuration;
using JiApp.Common.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Api.Features.Auth.Me;

public static class MeEndpoint
{
    public static IEndpointRouteBuilder MapMe(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/auth/me", async (
                MeHandler handler) =>
            {
                var result = await handler.HandleAsync();
                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                return Results.Json(
                    new ApiErrorResponse(Error: "Token invalid or expired"),
                    statusCode: StatusCodes.Status401Unauthorized);
            })
            .WithTags(SwaggerConstants.Tags.Auth)
            .WithSummary("Get current authenticated user")
            .Produces<MeResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicyNames.Me)
            .HasApiVersion(1);

        return endpoints;
    }
}