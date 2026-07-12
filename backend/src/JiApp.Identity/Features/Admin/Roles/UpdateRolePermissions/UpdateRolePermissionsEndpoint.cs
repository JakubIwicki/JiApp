using FluentValidation;
using JiApp.Common.Abstractions;
using JiApp.Identity.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Identity.Features.Admin.Roles.UpdateRolePermissions;

public static class UpdateRolePermissionsEndpoint
{
	public static IEndpointRouteBuilder MapUpdateRolePermissions(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapPut("/roles/{roleName}/permissions", async (
				string roleName,
				UpdateRolePermissionsRequest request,
				IValidator<UpdateRolePermissionsRequest> validator,
				UpdateRolePermissionsHandler handler,
				CancellationToken ct) =>
			{
				var validationResult = await validator.ValidateAsync(request, ct);
				if (!validationResult.IsValid)
					return Results.Extensions.ValidationError(validationResult.ErrorMessages());

				var result = await handler.HandleAsync(roleName, request, ct);
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
			.WithSummary("Update a role's permissions (full replacement, not delta)")
			.Produces(StatusCodes.Status200OK)
			.Produces<ApiErrorResponse>(StatusCodes.Status403Forbidden)
			.Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
			.ProducesValidationProblem();

		return endpoints;
	}
}
