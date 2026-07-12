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
			.WithSummary("Delete a user account")
			.Produces(StatusCodes.Status200OK)
			.Produces<ApiErrorResponse>(StatusCodes.Status403Forbidden)
			.Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

		return endpoints;
	}
}
