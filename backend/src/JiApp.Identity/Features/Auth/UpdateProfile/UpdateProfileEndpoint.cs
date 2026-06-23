using FluentValidation;
using JiApp.Common.Abstractions;
using JiApp.Identity.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Identity.Features.Auth.UpdateProfile;

public static class UpdateProfileEndpoint
{
    public static IEndpointRouteBuilder MapUpdateProfile(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPatch("/profile", async (
                UpdateProfileRequest request,
                IValidator<UpdateProfileRequest> validator,
                UpdateProfileHandler handler) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return Results.Extensions.ValidationError(validationResult.ErrorMessages());
                }

                var result = await handler.HandleAsync(request);
                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                var statusCode = result.ErrorCategory switch
                {
                    ResultCategories.NotFound => StatusCodes.Status404NotFound,
                    ResultCategories.Conflict => StatusCodes.Status409Conflict,
                    _ => StatusCodes.Status400BadRequest,
                };

                return Results.Json(
                    new ApiErrorResponse(Error: result.Error ?? ApiErrorResponse.UnknownErrorMessage),
                    statusCode: statusCode);
            })
            .WithTags(SwaggerConstants.Tags.Auth)
            .WithSummary("Update current user profile")
            .Produces<UpdateProfileResponse>()
            .ProducesValidationProblem()
            .RequireAuthorization()
            .RequireRateLimiting("Login");

        return endpoints;
    }
}
