using JiApp.Common.Abstractions;
using JiApp.Identity.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Identity.Features.Admin.Users.EnableUser;

public static class EnableUserEndpoint
{
	public static IEndpointRouteBuilder MapEnableUser(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapPost("/users/{userId:long}/enable", async (
				long userId,
				EnableUserHandler handler) =>
			{
				var result = await handler.HandleAsync(userId);
				if (result.IsSuccess)
					return Results.Ok();

				return Results.Json(
					new ApiErrorResponse(Error: result.Error ?? ApiErrorResponse.UnknownErrorMessage),
					statusCode: StatusCodes.Status404NotFound);
			})
			.WithTags(SwaggerConstants.Tags.Admin)
			.WithSummary("Enable a previously disabled user account")
			.Produces(StatusCodes.Status200OK)
			.Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

		return endpoints;
	}
}
