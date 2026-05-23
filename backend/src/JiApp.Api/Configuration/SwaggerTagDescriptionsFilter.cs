using System.Collections.Generic;
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
    }
}
