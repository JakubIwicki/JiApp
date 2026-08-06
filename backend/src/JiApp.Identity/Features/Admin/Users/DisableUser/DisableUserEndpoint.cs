using JiApp.Common.Abstractions;
using JiApp.Identity.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Identity.Features.Admin.Users.DisableUser;

public static class DisableUserEndpoint
{
    public static IEndpointRouteBuilder MapDisableUser(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/users/{userId:long}/disable", async (
                long userId,
                DisableUserHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(userId, ct);
                return result.IsSuccess
                    ? Results.Ok()
                    : result.ToHttp();
            })
            .WithTags(SwaggerConstants.Tags.Admin)
            .WithSummary("Disable a user account (lockout + revoke tokens)")
            .Produces(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
