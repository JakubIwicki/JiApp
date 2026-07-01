using FluentValidation;
using JiApp.Common.Abstractions;
using JiApp.Identity.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Identity.Features.Admin.Roles.CreateRole;

public static class CreateRoleEndpoint
{
	public static IEndpointRouteBuilder MapCreateRole(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapPost("/roles", async (
				CreateRoleRequest request,
				IValidator<CreateRoleRequest> validator,
				CreateRoleHandler handler) =>
			{
				var validationResult = await validator.ValidateAsync(request);
				if (!validationResult.IsValid)
					return Results.Extensions.ValidationError(validationResult.ErrorMessages());

				var result = await handler.HandleAsync(request);
				if (result.IsSuccess)
					return Results.Created((Uri?)null, null);

				var statusCode = result.ErrorCategory switch
				{
					ResultCategories.Conflict => StatusCodes.Status409Conflict,
					_ => StatusCodes.Status400BadRequest,
				};

				return Results.Json(
					new ApiErrorResponse(Error: result.Error ?? ApiErrorResponse.UnknownErrorMessage),
					statusCode: statusCode);
			})
			.WithTags(SwaggerConstants.Tags.Admin)
			.WithSummary("Create a new role with specified permissions")
			.Produces(StatusCodes.Status201Created)
			.Produces<ApiErrorResponse>(StatusCodes.Status409Conflict)
			.ProducesValidationProblem();

		return endpoints;
	}
}
