using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace JiApp.Api.Configuration;

public sealed class SwaggerTagDescriptionsFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags = new HashSet<OpenApiTag>
        {
            new() { Name = SwaggerConstants.Tags.Auth, Description = SwaggerConstants.TagDescriptions.Auth },
            new() { Name = SwaggerConstants.Tags.Search, Description = SwaggerConstants.TagDescriptions.Search },
            new() { Name = SwaggerConstants.Tags.Downloads, Description = SwaggerConstants.TagDescriptions.Downloads },
            new() { Name = SwaggerConstants.Tags.History, Description = SwaggerConstants.TagDescriptions.History },
            new() { Name = SwaggerConstants.Tags.System, Description = SwaggerConstants.TagDescriptions.System },
        };

        SubstituteApiVersionPath(swaggerDoc);
    }

    private static void SubstituteApiVersionPath(OpenApiDocument swaggerDoc)
    {
        var versionedPaths = swaggerDoc.Paths
            .Where(kvp => kvp.Key.Contains("{version}"))
            .ToDictionary(
                kvp => kvp.Key.Replace("v{version}", "v1"),
                kvp => kvp.Value);

        foreach (var (oldPath, _) in swaggerDoc.Paths
                     .Where(kvp => kvp.Key.Contains("{version}"))
                     .ToList())
        {
            swaggerDoc.Paths.Remove(oldPath);
        }

        foreach (var (newPath, pathItem) in versionedPaths)
        {
            swaggerDoc.Paths[newPath] = pathItem;
        }
    }
}