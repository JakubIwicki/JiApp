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
                LogoutHandler handler,
                CancellationToken ct) =>
            {
                var validationResult = await validator.ValidateAsync(request, ct);
                if (!validationResult.IsValid)
                {
                    return Results.Extensions.ValidationError(validationResult.ErrorMessages());
                }

                await handler.HandleAsync(request, ct);
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