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
				var p = Math.Max(1, page ?? 1);
				var ps = Math.Clamp(pageSize ?? 20, 1, 100);

				var result = await handler.HandleAsync(search, p, ps, ct);
				return Results.Ok(result.Value);
			})
			.WithTags(SwaggerConstants.Tags.Admin)
			.WithSummary("List users with optional search and pagination")
			.Produces<ListUsersResponse>();

		return endpoints;
	}
}
