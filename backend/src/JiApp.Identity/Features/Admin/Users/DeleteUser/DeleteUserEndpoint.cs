using JiApp.Common.Abstractions;
using JiApp.Identity.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Identity.Features.Admin.Users.DeleteUser;

public static class DeleteUserEndpoint
{
    public static IEndpointRouteBuilder MapDeleteUser(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/users/{userId:long}", async (
                long userId,
                DeleteUserHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(userId, ct);
                return result.IsSuccess
                    ? Results.Ok()
                    : result.ToHttp();
            })
            .WithTags(SwaggerConstants.Tags.Admin)
            .WithSummary("Delete a user account")
            .Produces(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
