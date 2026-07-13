using JiApp.Common.Abstractions;
using JiApp.Identity.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Identity.Features.Admin.Users.RemoveRole;

public static class RemoveRoleEndpoint
{
	public static IEndpointRouteBuilder MapRemoveRole(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapDelete("/users/{userId:long}/roles/{roleName}", async (
				long userId,
				string roleName,
				RemoveRoleHandler handler,
				CancellationToken ct) =>
			{
				var result = await handler.HandleAsync(userId, roleName, ct);
				if (result.IsSuccess)
					return Results.Ok();

				var statusCode = result.ErrorCategory switch
				{
					ResultCategories.NotFound => StatusCodes.Status404NotFound,
					ResultCategories.AccessDenied => StatusCodes.Status403Forbidden,
					_ => StatusCodes.Status400BadRequest,
				};

				return Results.Json(
					new ApiErrorResponse(Error: result.Error ?? ApiErrorResponse.UnknownErrorMessage),
					statusCode: statusCode);
			})
			.WithTags(SwaggerConstants.Tags.Admin)
			.WithSummary("Remove a role from a user")
			.Produces(StatusCodes.Status200OK)
			.Produces<ApiErrorResponse>(StatusCodes.Status403Forbidden)
			.Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

		return endpoints;
	}
}
