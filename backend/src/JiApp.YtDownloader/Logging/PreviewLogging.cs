namespace JiApp.YtDownloader.Logging;

internal static partial class PreviewLogging
{
    [LoggerMessage(EventId = 6000, Level = LogLevel.Warning,
        Message = "Failed to resolve audio URL for video {VideoId}")]
    public static partial void PreviewResolveFailed(this ILogger logger, Exception ex, string videoId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug,
        Message = "Preview cache miss for video {VideoId}")]
    public static partial void PreviewCacheMiss(this ILogger logger, string videoId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug,
        Message = "Preview cache hit for video {VideoId}")]
    public static partial void PreviewCacheHit(this ILogger logger, string videoId);
}