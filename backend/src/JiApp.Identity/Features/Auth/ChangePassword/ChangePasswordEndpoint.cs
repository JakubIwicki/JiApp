using FluentValidation;
using JiApp.Common.Abstractions;
using JiApp.Identity.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Identity.Features.Auth.ChangePassword;

public static class ChangePasswordEndpoint
{
    public static IEndpointRouteBuilder MapChangePassword(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/change-password", async (
                ChangePasswordRequest request,
                IValidator<ChangePasswordRequest> validator,
                ChangePasswordHandler handler) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return Results.Extensions.ValidationError(validationResult.ErrorMessages());
                }

                var result = await handler.HandleAsync(request);
                if (result.IsSuccess)
                    return Results.Ok();

                return Results.Json(
                    new ApiErrorResponse(Error: result.Error ?? ApiErrorResponse.UnknownErrorMessage),
                    statusCode: StatusCodes.Status400BadRequest);
            })
            .WithTags(SwaggerConstants.Tags.Auth)
            .WithSummary("Change current user password")
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .RequireAuthorization()
            .RequireRateLimiting("Login");

        return endpoints;
    }
}
