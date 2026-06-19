namespace JiApp.YtDownloader.Logging;

internal static partial class HandlerLogs
{
    [LoggerMessage(EventId = 13, Level = LogLevel.Information, Message = "Download requested for file {FileId}")]
    public static partial void DownloadRequestedForFile(this ILogger logger, string fileId);

    [LoggerMessage(EventId = 14, Level = LogLevel.Warning,
        Message = "File {FileId} expired or not found for user {UserId}")]
    public static partial void FileExpiredOrNotFound(this ILogger logger, string fileId, long userId);

    [LoggerMessage(EventId = 15, Level = LogLevel.Information, Message = "Fetching download history (limit: {Limit})")]
    public static partial void FetchingDownloadHistory(this ILogger logger, int limit);

    [LoggerMessage(EventId = 16, Level = LogLevel.Error, Message = "Download failed for video {VideoId}")]
    public static partial void DownloadFailedForVideo(this ILogger logger, Exception exception, string videoId);

    [LoggerMessage(EventId = 17, Level = LogLevel.Information, Message = "Fetching search history (limit: {Limit})")]
    public static partial void FetchingSearchHistory(this ILogger logger, int limit);

    [LoggerMessage(EventId = 18, Level = LogLevel.Information, Message = "Fetching combined history (limit: {Limit})")]
    public static partial void FetchingCombinedHistory(this ILogger logger, int limit);

    [LoggerMessage(EventId = 19, Level = LogLevel.Warning, Message = "Failed to retrieve search history")]
    public static partial void FailedToRetrieveSearchHistory(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 20, Level = LogLevel.Warning, Message = "Failed to retrieve download history")]
    public static partial void FailedToRetrieveDownloadHistory(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 21, Level = LogLevel.Error, Message = "Both search and download history retrieval failed")]
    public static partial void BothHistoryRetrievalsFailed(this ILogger logger);

    [LoggerMessage(EventId = 22, Level = LogLevel.Warning, Message = "Failed to save search history for user {UserId}")]
    public static partial void FailedToSaveSearchHistory(this ILogger logger, Exception exception, long userId);

    [LoggerMessage(EventId = 25, Level = LogLevel.Warning,
        Message = "yt-dlp download failed for video {VideoId}: {Errors}")]
    public static partial void YoutubeDlDownloadFailed(this ILogger logger, string videoId, string errors);

    [LoggerMessage(EventId = 30, Level = LogLevel.Error,
        Message = "Assistant chat stream failed for user {UserId}: {ExceptionType} {ExceptionMessage}")]
    public static partial void AssistantChatStreamFailed(
        this ILogger logger, long userId, string exceptionType, string exceptionMessage);

    [LoggerMessage(EventId = 31, Level = LogLevel.Warning,
        Message = "Failed to deserialize tool result for {ToolName}; skipping structured event")]
    public static partial void AssistantToolResultDeserializeFailed(
        this ILogger logger, Exception exception, string toolName);

    [LoggerMessage(EventId = 32, Level = LogLevel.Warning,
        Message = "Assistant chat turn timed out for user {UserId} after {TimeoutSeconds}s")]
    public static partial void AssistantChatTurnTimedOut(
        this ILogger logger, long userId, int timeoutSeconds);
}