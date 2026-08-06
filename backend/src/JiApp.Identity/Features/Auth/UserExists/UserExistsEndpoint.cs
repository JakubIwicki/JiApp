using JiApp.Common.Models;
using JiApp.Identity.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Identity.Features.Auth.UserExists;

public static class UserExistsEndpoint
{
    public static IEndpointRouteBuilder MapUserExists(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/users/{userId:long}/exists", async (
                long userId,
                UserManager<User> userManager) =>
            {
                var user = await userManager.FindByIdAsync(
                    userId.ToString(System.Globalization.CultureInfo.InvariantCulture));
                return user is null
                    ? Results.NotFound()
                    : Results.Ok();
            })
            .WithTags(SwaggerConstants.Tags.Auth)
            .WithSummary("Check whether a user exists (cross-service board-member existence probe)")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        return endpoints;
    }
}
