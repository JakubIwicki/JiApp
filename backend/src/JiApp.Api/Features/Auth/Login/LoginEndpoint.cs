using FluentValidation;
using JiApp.Api.Configuration;
using JiApp.Common.Abstractions;

namespace JiApp.Api.Features.Auth.Login;

public static class LoginEndpoint
{
    public static IEndpointRouteBuilder MapLogin(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/login", async (
            LoginRequest request,
            IValidator<LoginRequest> validator,
            LoginHandler handler) =>
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["errors"] = errors });
            }

            var result = await handler.HandleAsync(request);
            if (result.IsSuccess)
                return Results.Ok(result.Value);

            return Results.Json(
                new ApiErrorResponse(Error: "Invalid credentials"),
                statusCode: StatusCodes.Status401Unauthorized);
        })
        .WithTags("Auth")
        .WithSummary("Authenticate and receive a JWT bearer token")
        .Produces<LoginResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .RequireRateLimiting(RateLimitPolicyNames.Login)
        .AllowAnonymous();

        return endpoints;
    }
}
