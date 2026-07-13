using FluentValidation;
using JiApp.Common.Abstractions;
using JiApp.Identity.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Identity.Features.Auth.Register;

public static class RegisterEndpoint
{
    public static IEndpointRouteBuilder MapRegister(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/register", async (
                RegisterRequest request,
                IValidator<RegisterRequest> validator,
                RegisterHandler handler,
                CancellationToken ct) =>
            {
                var validationResult = await validator.ValidateAsync(request, ct);
                if (!validationResult.IsValid)
                {
                    return Results.Extensions.ValidationError(validationResult.ErrorMessages());
                }

                var result = await handler.HandleAsync(request, ct);
                if (result.IsSuccess)
                    return Results.Created((Uri?)null, result.Value);

                return Results.Json(new ApiErrorResponse(Error: result.Error ?? ApiErrorResponse.UnknownErrorMessage),
                    statusCode: StatusCodes.Status400BadRequest);
            })
            .WithTags(SwaggerConstants.Tags.Auth)
            .WithSummary("Register a new user account")
            .Produces<RegisterResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .RequireRateLimiting("Register")
            .AllowAnonymous();

        return endpoints;
    }
}