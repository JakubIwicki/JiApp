using FluentValidation;
using JiApp.Common.Abstractions;
using JiApp.Identity.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Identity.Features.Admin.Users.AssignRole;

public static class AssignRoleEndpoint
{
	public static IEndpointRouteBuilder MapAssignRole(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapPost("/users/{userId:long}/roles", async (
				long userId,
				AssignRoleRequest request,
				IValidator<AssignRoleRequest> validator,
				AssignRoleHandler handler,
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
					ResultCategories.Validation => StatusCodes.Status400BadRequest,
					_ => StatusCodes.Status400BadRequest,
				};

				return Results.Json(
					new ApiErrorResponse(Error: result.Error ?? ApiErrorResponse.UnknownErrorMessage),
					statusCode: statusCode);
			})
			.WithTags(SwaggerConstants.Tags.Admin)
			.WithSummary("Assign a role to a user")
			.Produces(StatusCodes.Status200OK)
			.ProducesValidationProblem()
			.Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

		return endpoints;
	}
}
