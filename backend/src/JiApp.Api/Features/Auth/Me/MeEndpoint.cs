using JiApp.Common.Abstractions;

namespace JiApp.Api.Features.Auth.Me;

public static class MeEndpoint
{
    public static IEndpointRouteBuilder MapMe(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/auth/me", async (
            MeHandler handler) =>
        {
            var result = await handler.HandleAsync();
            if (result.IsSuccess)
                return Results.Ok(result.Value);

            return Results.Json(
                new ApiErrorResponse(Error: "Token invalid or expired"),
                statusCode: StatusCodes.Status401Unauthorized);
        })
        .WithTags("Auth")
        .WithSummary("Get current authenticated user")
        .Produces<MeResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();

        return endpoints;
    }
}
