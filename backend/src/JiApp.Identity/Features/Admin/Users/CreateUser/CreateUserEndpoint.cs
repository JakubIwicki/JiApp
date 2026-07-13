using FluentValidation;
using JiApp.Common.Abstractions;
using JiApp.Identity.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Identity.Features.Admin.Users.CreateUser;

public static class CreateUserEndpoint
{
	public static IEndpointRouteBuilder MapCreateUser(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapPost("/users", async (
				CreateUserRequest request,
				IValidator<CreateUserRequest> validator,
				CreateUserHandler handler,
				CancellationToken ct) =>
			{
				var validationResult = await validator.ValidateAsync(request, ct);
				if (!validationResult.IsValid)
					return Results.Extensions.ValidationError(validationResult.ErrorMessages());

				var result = await handler.HandleAsync(request, ct);
				if (result.IsSuccess)
					return Results.Created((Uri?)null, result.Value);

				return Results.Json(
					new ApiErrorResponse(Error: result.Error ?? ApiErrorResponse.UnknownErrorMessage),
					statusCode: StatusCodes.Status400BadRequest);
			})
			.WithTags(SwaggerConstants.Tags.Admin)
			.WithSummary("Create a new user with optional role assignments")
			.Produces<CreateUserResponse>(StatusCodes.Status201Created)
			.ProducesValidationProblem();

		return endpoints;
	}
}
