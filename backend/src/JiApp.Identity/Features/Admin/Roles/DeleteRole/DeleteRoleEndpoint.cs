using JiApp.Common.Abstractions;
using JiApp.Identity.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Identity.Features.Admin.Roles.DeleteRole;

public static class DeleteRoleEndpoint
{
	public static IEndpointRouteBuilder MapDeleteRole(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapDelete("/roles/{roleName}", async (
				string roleName,
				DeleteRoleHandler handler,
				CancellationToken ct) =>
			{
				var result = await handler.HandleAsync(roleName, ct);
				if (result.IsSuccess)
					return Results.Ok();

				var statusCode = result.ErrorCategory switch
				{
					ResultCategories.NotFound => StatusCodes.Status404NotFound,
					ResultCategories.AccessDenied => StatusCodes.Status403Forbidden,
					ResultCategories.Conflict => StatusCodes.Status409Conflict,
					_ => StatusCodes.Status400BadRequest,
				};

				return Results.Json(
					new ApiErrorResponse(Error: result.Error ?? ApiErrorResponse.UnknownErrorMessage),
					statusCode: statusCode);
			})
			.WithTags(SwaggerConstants.Tags.Admin)
			.WithSummary("Delete a role (reserved roles are protected)")
			.Produces(StatusCodes.Status200OK)
			.Produces<ApiErrorResponse>(StatusCodes.Status403Forbidden)
			.Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
			.Produces<ApiErrorResponse>(StatusCodes.Status409Conflict);

		return endpoints;
	}
}
