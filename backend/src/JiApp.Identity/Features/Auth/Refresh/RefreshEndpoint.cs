using FluentValidation;
using JiApp.Common.Abstractions;
using JiApp.Identity.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.Identity.Features.Auth.Refresh;

public static class RefreshEndpoint
{
    public static IEndpointRouteBuilder MapRefresh(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/refresh", async (
                RefreshRequest request,
                IValidator<RefreshRequest> validator,
                RefreshHandler handler,
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
                    new ApiErrorResponse(Error: "Invalid or expired refresh token"),
                    statusCode: StatusCodes.Status401Unauthorized);
            })
            .WithTags(SwaggerConstants.Tags.Auth)
            .WithSummary("Refresh JWT token using refresh token")
            .Produces<RefreshResponse>()
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem()
            .RequireRateLimiting("Refresh")
            .AllowAnonymous();

        return endpoints;
    }
}