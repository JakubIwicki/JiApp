using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using JiApp.Api.Configuration;
using JiApp.Common.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Api.Features.Auth.Register;

public static class RegisterEndpoint
{
    public static IEndpointRouteBuilder MapRegister(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/register", async (
                RegisterRequest request,
                IValidator<RegisterRequest> validator,
                RegisterHandler handler) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["errors"] = errors });
                }

                var result = await handler.HandleAsync(request);
                if (result.IsSuccess)
                    return Results.Created("/api/v1/auth/me", result.Value);

                return Results.Json(new ApiErrorResponse(Error: result.Error ?? "An unknown error occurred"),
                    statusCode: StatusCodes.Status400BadRequest);
            })
            .WithTags(SwaggerConstants.Tags.Auth)
            .WithSummary("Register a new user account")
            .Produces<RegisterResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .RequireRateLimiting(RateLimitPolicyNames.Register)
            .AllowAnonymous()
            .HasApiVersion(1);

        return endpoints;
    }
}