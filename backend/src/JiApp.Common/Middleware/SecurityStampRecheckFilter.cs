using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using Microsoft.AspNetCore.Http;

namespace JiApp.Common.Middleware;

public sealed class SecurityStampRecheckFilter(ISecurityStampValidator validator) : IEndpointFilter
{
	public async ValueTask<object?> InvokeAsync(
		EndpointFilterInvocationContext context, EndpointFilterDelegate next)
	{
		var result = await validator.ValidateCurrentAsync(context.HttpContext.RequestAborted);

		return result switch
		{
			StampValidationResult.Valid => await next(context),
			StampValidationResult.Revoked => Results.Json(
				new ApiErrorResponse(Error: "Token has been revoked"),
				ApiErrorResponse.JsonOptions,
				statusCode: StatusCodes.Status401Unauthorized),
			StampValidationResult.Unavailable => Results.Json(
				new ApiErrorResponse(Error: "Authorization service unavailable"),
				ApiErrorResponse.JsonOptions,
				statusCode: StatusCodes.Status503ServiceUnavailable),
			_ => await next(context)
		};
	}
}
