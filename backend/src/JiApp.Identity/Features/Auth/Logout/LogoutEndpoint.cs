using FluentValidation;
using JiApp.Common.Abstractions;
using JiApp.Identity.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Identity.Features.Auth.Logout;

public static class LogoutEndpoint
{
    public static IEndpointRouteBuilder MapLogout(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/logout", async (
                LogoutRequest request,
                IValidator<LogoutRequest> validator,
                LogoutHandler handler) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return Results.Extensions.ValidationError(validationResult.ErrorMessages());
                }

                await handler.HandleAsync(request);
                return Results.Ok();
            })
            .WithTags(SwaggerConstants.Tags.Auth)
            .WithSummary("Logout and revoke refresh token (access token remains valid until expiry)")
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .RequireRateLimiting("Logout")
            .AllowAnonymous();

        return endpoints;
    }
}