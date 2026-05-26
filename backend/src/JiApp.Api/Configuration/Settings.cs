using System;
using System.Collections.Generic;

namespace JiApp.Api.Configuration;

public sealed class Settings
{
    public string? ConnectionString { get; set; }
    public AppSettings? App { get; init; }
    public JwtSettings? Jwt { get; set; }
    public YoutubeSettings? Youtube { get; set; }
    public RateLimitingOptions? RateLimiting { get; set; }

    public void Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(ConnectionString))
            errors.Add("ConnectionString is not configured.");

        ValidateApp(errors);
        ValidateJwt(errors);
        ValidateYoutube(errors);
        ValidateRateLimiting(errors);

        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"Configuration validation failed:\n{string.Join("\n", errors)}");
    }

    private void ValidateApp(List<string> errors)
    {
        if (App is null)
            errors.Add("App section is not configured.");
        else if (string.IsNullOrEmpty(App.BaseDirectory))
            errors.Add("App:BaseDirectory is not configured.");
    }

    private void ValidateJwt(List<string> errors)
    {
        if (Jwt is null)
        {
            errors.Add("Jwt section is not configured.");
            return;
        }

        if (string.IsNullOrEmpty(Jwt.Key))
            errors.Add("Jwt:Key is not configured.");
        if (string.IsNullOrEmpty(Jwt.Issuer))
            errors.Add("Jwt:Issuer is not configured.");
        if (string.IsNullOrEmpty(Jwt.Audience))
            errors.Add("Jwt:Audience is not configured.");
        if (!Jwt.ExpireMinutes.HasValue)
            errors.Add("Jwt:ExpireMinutes is not configured.");
        else if (Jwt.ExpireMinutes.Value <= 0)
            errors.Add("Jwt:ExpireMinutes must be greater than 0.");
    }

    private void ValidateYoutube(List<string> errors)
    {
        if (Youtube is null)
        {
            errors.Add("Youtube section is not configured.");
            return;
        }

        if (string.IsNullOrEmpty(Youtube.ApiKey))
            errors.Add("Youtube:ApiKey is not configured.");
        if (string.IsNullOrEmpty(Youtube.YtDlpPath))
            errors.Add("Youtube:YtDlpPath is not configured.");
        if (string.IsNullOrEmpty(Youtube.FfmpegPath))
            errors.Add("Youtube:FfmpegPath is not configured.");
    }

    private void ValidateRateLimiting(List<string> errors)
    {
        if (RateLimiting is null)
        {
            errors.Add("RateLimiting section is not configured.");
            return;
        }

        ValidatePolicy(errors, RateLimiting.Login, "RateLimiting:Login");
        ValidatePolicy(errors, RateLimiting.Register, "RateLimiting:Register");
        ValidatePolicy(errors, RateLimiting.Health, "RateLimiting:Health");
        ValidatePolicy(errors, RateLimiting.DownloadFile, "RateLimiting:DownloadFile");
        ValidatePolicy(errors, RateLimiting.SearchVideos, "RateLimiting:SearchVideos");
        ValidatePolicy(errors, RateLimiting.SearchHistory, "RateLimiting:SearchHistory");
        ValidatePolicy(errors, RateLimiting.DownloadHistory, "RateLimiting:DownloadHistory");
        ValidatePolicy(errors, RateLimiting.GetHistory, "RateLimiting:GetHistory");
        ValidatePolicy(errors, RateLimiting.Me, "RateLimiting:Me");
        ValidatePolicy(errors, RateLimiting.GetDownloadLink, "RateLimiting:GetDownloadLink");
    }

    private static void ValidatePolicy(List<string> errors, RateLimitPolicyOptions? policy, string name)
    {
        if (policy is null)
        {
            errors.Add($"{name} is not configured.");
            return;
        }

        if (!policy.PermitLimit.HasValue || policy.PermitLimit.Value <= 0)
            errors.Add($"{name}:PermitLimit must be greater than 0.");
        if (!policy.WindowInSeconds.HasValue || policy.WindowInSeconds.Value <= 0)
            errors.Add($"{name}:WindowInSeconds must be greater than 0.");
        if (!policy.QueueLimit.HasValue)
            errors.Add($"{name}:QueueLimit is not configured.");
        if (!policy.SegmentsPerWindow.HasValue)
            errors.Add($"{name}:SegmentsPerWindow is not configured.");
    }

    [Serializable]
    public sealed class AppSettings
    {
        public string? BaseDirectory { get; set; }
    }

    [Serializable]
    public sealed class JwtSettings
    {
        public string? Key { get; set; }
        public string? Issuer { get; set; }
        public string? Audience { get; set; }
        public int? ExpireMinutes { get; set; }
    }

    [Serializable]
    public sealed class YoutubeSettings
    {
        public string? ApiKey { get; set; }
        public string? YtDlpPath { get; set; }
        public string? FfmpegPath { get; set; }
    }

    [Serializable]
    public sealed class RateLimitingOptions
    {
        public RateLimitPolicyOptions? Login { get; set; }
        public RateLimitPolicyOptions? Register { get; set; }
        public RateLimitPolicyOptions? Health { get; set; }
        public RateLimitPolicyOptions? DownloadFile { get; set; }
        public RateLimitPolicyOptions? SearchVideos { get; set; }
        public RateLimitPolicyOptions? SearchHistory { get; set; }
        public RateLimitPolicyOptions? DownloadHistory { get; set; }
        public RateLimitPolicyOptions? GetHistory { get; set; }
        public RateLimitPolicyOptions? Me { get; set; }
        public RateLimitPolicyOptions? GetDownloadLink { get; set; }
        public RateLimitPolicyOptions? Preview { get; set; }
    }

    [Serializable]
    public sealed class RateLimitPolicyOptions
    {
        public int? PermitLimit { get; set; }
        public int? WindowInSeconds { get; set; }
        public int? QueueLimit { get; set; }
        public int? SegmentsPerWindow { get; set; }
    }
}

public static class RateLimitPolicyNames
{
    public const string Login = "LoginPolicy";
    public const string Register = "RegisterPolicy";
    public const string Health = "HealthPolicy";
    public const string DownloadFile = "DownloadFilePolicy";
    public const string SearchVideos = "SearchVideosPolicy";
    public const string SearchHistory = "SearchHistoryPolicy";
    public const string DownloadHistory = "DownloadHistoryPolicy";
    public const string GetHistory = "GetHistoryPolicy";
    public const string Me = "MePolicy";
    public const string GetDownloadLink = "GetDownloadLinkPolicy";
    public const string Preview = "PreviewPolicy";
}