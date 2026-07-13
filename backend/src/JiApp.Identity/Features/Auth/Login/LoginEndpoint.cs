using FluentValidation;
using JiApp.Common.Abstractions;
using JiApp.Identity.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Identity.Features.Auth.Login;

public static class LoginEndpoint
{
    public static IEndpointRouteBuilder MapLogin(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/login", async (
                LoginRequest request,
                IValidator<LoginRequest> validator,
                LoginHandler handler,
                CancellationToken ct) =>
            {
                var validationResult = await validator.ValidateAsync(request, ct);
                if (!validationResult.IsValid)
                {
                    return Results.Extensions.ValidationError(validationResult.ErrorMessages());
                }

                var result = await handler.HandleAsync(request, ct);
                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                return Results.Json(
                    new ApiErrorResponse(Error: "Invalid credentials"),
                    statusCode: StatusCodes.Status401Unauthorized);
            })
            .WithTags(SwaggerConstants.Tags.Auth)
            .WithSummary("Authenticate and receive a JWT bearer token")
            .Produces<LoginResponse>()
            .ProducesValidationProblem()
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .RequireRateLimiting("Login")
            .AllowAnonymous();

        return endpoints;
    }
}