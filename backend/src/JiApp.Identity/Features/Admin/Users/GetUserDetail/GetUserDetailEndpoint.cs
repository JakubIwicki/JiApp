using JiApp.Common.Abstractions;
using JiApp.Identity.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Identity.Features.Admin.Users.GetUserDetail;

public static class GetUserDetailEndpoint
{
	public static IEndpointRouteBuilder MapGetUserDetail(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapGet("/users/{userId:long}", async (
				long userId,
				GetUserDetailHandler handler,
				CancellationToken ct) =>
			{
				var result = await handler.HandleAsync(userId, ct);
				if (result.IsSuccess)
					return Results.Ok(result.Value);

				return Results.Json(
					new ApiErrorResponse(Error: result.Error ?? ApiErrorResponse.UnknownErrorMessage),
					statusCode: StatusCodes.Status404NotFound);
			})
			.WithTags(SwaggerConstants.Tags.Admin)
			.WithSummary("Get user details by ID")
			.Produces<GetUserDetailResponse>()
			.Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

		return endpoints;
	}
}
