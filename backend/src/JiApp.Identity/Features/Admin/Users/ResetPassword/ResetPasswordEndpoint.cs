using FluentValidation;
using JiApp.Common.Abstractions;
using JiApp.Identity.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Identity.Features.Admin.Users.ResetPassword;

public static class ResetPasswordEndpoint
{
	public static IEndpointRouteBuilder MapResetPassword(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapPost("/users/{userId:long}/reset-password", async (
				long userId,
				ResetPasswordRequest request,
				IValidator<ResetPasswordRequest> validator,
				ResetPasswordHandler handler,
				CancellationToken ct) =>
			{
				var validationResult = await validator.ValidateAsync(request, ct);
				if (!validationResult.IsValid)
					return Results.Extensions.ValidationError(validationResult.ErrorMessages());

				var result = await handler.HandleAsync(userId, request, ct);
				if (result.IsSuccess)
					return Results.Ok();

				var statusCode = result.ErrorCategory switch
				{
					ResultCategories.NotFound => StatusCodes.Status404NotFound,
					_ => StatusCodes.Status400BadRequest,
				};

				return Results.Json(
					new ApiErrorResponse(Error: result.Error ?? ApiErrorResponse.UnknownErrorMessage),
					statusCode: statusCode);
			})
			.WithTags(SwaggerConstants.Tags.Admin)
			.WithSummary("Admin reset a user's password (revokes all refresh tokens)")
			.Produces(StatusCodes.Status200OK)
			.Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
			.ProducesValidationProblem();

		return endpoints;
	}
}
