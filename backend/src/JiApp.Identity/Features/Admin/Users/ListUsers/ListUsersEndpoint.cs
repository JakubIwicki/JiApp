using JiApp.Common.Abstractions;
using JiApp.Identity.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Identity.Features.Admin.Users.ListUsers;

public static class ListUsersEndpoint
{
    public static IEndpointRouteBuilder MapListUsers(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/users", async (
                string? search,
                int? page,
                int? pageSize,
                ListUsersHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(search, page, pageSize, ct);
                return ToHttpResult(result);
            })
            .WithTags(SwaggerConstants.Tags.Admin)
            .WithSummary("List users with optional search and pagination")
            .Produces<ListUsersResponse>();

        return endpoints;
    }

    // The handler always succeeds today; the guard is defensive so a future failure
    // never dereferences result.Value. A failure here is an unexpected server fault.
    internal static IResult ToHttpResult(Result<ListUsersResponse> result) =>
        result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error, statusCode: StatusCodes.Status500InternalServerError);
}
