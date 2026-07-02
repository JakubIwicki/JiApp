using JiApp.Common.Abstractions;
using JiApp.Identity.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Identity.Features.Auth.Validate;

public static class ValidateEndpoint
{
	public static IEndpointRouteBuilder MapValidate(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapGet("/validate", () => Results.NoContent())
			.WithTags(SwaggerConstants.Tags.Auth)
			.WithSummary("Validate the current access token (security-stamp recheck)")
			.Produces(StatusCodes.Status204NoContent)
			.Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
			.RequireAuthorization();

		return endpoints;
	}
}
