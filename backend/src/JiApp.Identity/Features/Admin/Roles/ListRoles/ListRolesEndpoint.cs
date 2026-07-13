using JiApp.Identity.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Identity.Features.Admin.Roles.ListRoles;

public static class ListRolesEndpoint
{
	public static IEndpointRouteBuilder MapListRoles(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapGet("/roles", async (ListRolesHandler handler, CancellationToken ct) =>
			{
				var result = await handler.HandleAsync(ct);
				return Results.Ok(result.Value);
			})
			.WithTags(SwaggerConstants.Tags.Admin)
			.WithSummary("List all roles with their assigned permissions")
			.Produces<ListRolesResponse>();

		return endpoints;
	}
}
