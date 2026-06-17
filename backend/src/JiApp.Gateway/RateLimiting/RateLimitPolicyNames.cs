namespace JiApp.Gateway.RateLimiting;

public static class RateLimitPolicyNames
{
    // Auth
    public const string LoginPolicy = "LoginPolicy";
    public const string RegisterPolicy = "RegisterPolicy";
    public const string RefreshPolicy = "RefreshPolicy";
    public const string LogoutPolicy = "LogoutPolicy";
    public const string MePolicy = "MePolicy";

    // YT search
    public const string SearchVideosPolicy = "SearchVideosPolicy";

    // YT downloads
    public const string GetDownloadLinkPolicy = "GetDownloadLinkPolicy";
    public const string DownloadFilePolicy = "DownloadFilePolicy";

    // YT history
    public const string SearchHistoryPolicy = "SearchHistoryPolicy";
    public const string DownloadHistoryPolicy = "DownloadHistoryPolicy";
    public const string GetHistoryPolicy = "GetHistoryPolicy";

    // YT preview
    public const string PreviewPolicy = "PreviewPolicy";

    // Health
    public const string HealthPolicy = "HealthPolicy";

    // Scheduler
    public const string SchedulerPolicy = "SchedulerPolicy";

    // Dev-only (error testing)
    public const string ThrowPolicy = "ThrowPolicy";
}