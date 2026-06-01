namespace JiApp.Identity.Logging;

internal static partial class AuthLogging
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processing registration for {Username}")]
    public static partial void ProcessingRegistration(this ILogger logger, string username);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Registration failed for {Username} - {Errors}")]
    public static partial void RegistrationFailed(this ILogger logger, string username, string errors);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Registration completed for {Username}")]
    public static partial void RegistrationCompleted(this ILogger logger, string username);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Login attempt for {Username}")]
    public static partial void LoginAttempt(this ILogger logger, string username);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Login failed for {Username} - User not found")]
    public static partial void LoginFailedUserNotFound(this ILogger logger, string username);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Login failed for {Username} - Account locked")]
    public static partial void LoginFailedAccountLocked(this ILogger logger, string username);

    [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "Login failed for {Username} - Invalid password")]
    public static partial void LoginFailedInvalidPassword(this ILogger logger, string username);

    [LoggerMessage(EventId = 8, Level = LogLevel.Information, Message = "Login successful for {Username}")]
    public static partial void LoginSuccessful(this ILogger logger, string username);

    [LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = "Fetching current user")]
    public static partial void FetchingCurrentUser(this ILogger logger);

    [LoggerMessage(EventId = 10, Level = LogLevel.Warning, Message = "User not found for ID {UserId}")]
    public static partial void UserNotFoundForId(this ILogger logger, long userId);

    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Refresh token validated for user {UserId}")]
    public static partial void RefreshTokenValidated(this ILogger logger, long userId);

    [LoggerMessage(EventId = 12, Level = LogLevel.Warning, Message = "Refresh token invalid or expired")]
    public static partial void RefreshTokenInvalid(this ILogger logger);

    [LoggerMessage(EventId = 13, Level = LogLevel.Information, Message = "Refresh token revoked for token ID {TokenId}")]
    public static partial void RefreshTokenRevoked(this ILogger logger, long tokenId);

    [LoggerMessage(EventId = 14, Level = LogLevel.Critical,
        Message = "SECURITY ALERT: Refresh token reuse detected for token ID {TokenId}, user ID {UserId}. All tokens revoked for user.")]
    public static partial void RefreshTokenReuseDetected(this ILogger logger, long tokenId, long userId);
}
