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
                UpdateProfileHandler handler,
                CancellationToken ct) =>
            {
                var validationResult = await validator.ValidateAsync(request, ct);
                if (!validationResult.IsValid)
                {
                    return Results.Extensions.ValidationError(validationResult.ErrorMessages());
                }

                var result = await handler.HandleAsync(request, ct);
                return result.ToHttp();
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
