using FluentValidation;
using JiApp.Common.Abstractions;
using JiApp.YtDownloader.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace JiApp.YtDownloader.Features.ArchiveSearch;

public static class ArchiveSearchEndpoint
{
    public static IEndpointRouteBuilder MapArchiveSearch(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPatch("/search/history/{id:long}/archive", async (
                long id,
                IValidator<ArchiveSearchRequest> validator,
                ArchiveSearchHandler handler) =>
            {
                var request = new ArchiveSearchRequest(id);

                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
                    return Results.Extensions.ValidationError(errors);
                }

                var result = await handler.HandleAsync(request);
                if (result.IsSuccess)
                    return Results.Ok(result.Value);

                return Results.Json(new ApiErrorResponse(Error: result.Error ?? ApiErrorResponse.UnknownErrorMessage),
                    statusCode: StatusCodes.Status404NotFound);
            })
            .WithTags(SwaggerConstants.Tags.Search)
            .WithSummary("Archive a search history entry")
            .Produces<bool>()
            .ProducesValidationProblem()
            .RequireAuthorization();

        return endpoints;
    }
}