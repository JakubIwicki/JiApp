using System;
using Microsoft.Extensions.Logging;

namespace JiApp.Api.Logging;

internal static partial class PreviewLogging
{
    [LoggerMessage(EventId = 6000, Level = LogLevel.Warning,
        Message = "Failed to resolve audio URL for video {VideoId}")]
    public static partial void PreviewResolveFailed(this ILogger logger, Exception ex, string videoId);
}