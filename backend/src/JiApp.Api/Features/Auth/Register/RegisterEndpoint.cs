using FluentValidation;
using JiApp.Api.Configuration;
using JiApp.Common.Abstractions;

namespace JiApp.Api.Features.Auth.Register;

public static class RegisterEndpoint
{
    public static IEndpointRouteBuilder MapRegister(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/register", async (
            RegisterRequest request,
            IValidator<RegisterRequest> validator,
            RegisterHandler handler) =>
        {
            if (request is null)
                return Results.Json(new ApiErrorResponse(Error: "Request body cannot be null"), statusCode: StatusCodes.Status400BadRequest);

            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["errors"] = errors });
            }

            var result = await handler.HandleAsync(request);
            if (result.IsSuccess)
                return Results.Created("/api/auth/me", result.Value);

            return Results.Json(new ApiErrorResponse(Error: result.Error!), statusCode: StatusCodes.Status400BadRequest);
        })
        .WithTags("Auth")
        .WithSummary("Register a new user account")
        .Produces<RegisterResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .RequireRateLimiting(RateLimitPolicyNames.Register)
        .AllowAnonymous();

        return endpoints;
    }
}
