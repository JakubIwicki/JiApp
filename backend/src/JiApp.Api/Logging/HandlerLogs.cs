using System;
using Microsoft.Extensions.Logging;

namespace JiApp.Api.Logging;

internal static partial class HandlerLogs
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing registration for {Username}")]
    public static partial void ProcessingRegistration(this ILogger logger, string username);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning,
        Message = "Registration failed - Username {Username} already taken")]
    public static partial void RegistrationFailedUsernameTaken(this ILogger logger, string username);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning,
        Message = "Registration failed - Email {Email} already taken")]
    public static partial void RegistrationFailedEmailTaken(this ILogger logger, string email);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Registration failed for {Username} - {Errors}")]
    public static partial void RegistrationFailedWithErrors(this ILogger logger, string username, string errors);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Registration completed for {Username}")]
    public static partial void RegistrationCompleted(this ILogger logger, string username);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Login attempt for {Username}")]
    public static partial void LoginAttempt(this ILogger logger, string username);

    [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "Login failed for {Username} - User not found")]
    public static partial void LoginFailedUserNotFound(this ILogger logger, string username);

    [LoggerMessage(EventId = 8, Level = LogLevel.Warning, Message = "Login failed for {Username} - Account locked")]
    public static partial void LoginFailedAccountLocked(this ILogger logger, string username);

    [LoggerMessage(EventId = 9, Level = LogLevel.Warning, Message = "Login failed for {Username} - Invalid password")]
    public static partial void LoginFailedInvalidPassword(this ILogger logger, string username);

    [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = "Login successful for {Username}")]
    public static partial void LoginSuccessful(this ILogger logger, string username);

    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Fetching current user")]
    public static partial void FetchingCurrentUser(this ILogger logger);

    [LoggerMessage(EventId = 12, Level = LogLevel.Warning, Message = "User not found for ID {UserId}")]
    public static partial void UserNotFoundForId(this ILogger logger, long userId);

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

    [LoggerMessage(EventId = 23, Level = LogLevel.Warning, Message = "Cleanup expired temp files failed")]
    public static partial void CleanupExpiredTempFilesFailed(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 24, Level = LogLevel.Error, Message = "Unhandled exception occurred")]
    public static partial void UnhandledExceptionOccurred(this ILogger logger, Exception exception);
}