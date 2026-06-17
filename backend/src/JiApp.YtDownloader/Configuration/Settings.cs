namespace JiApp.YtDownloader.Configuration;

public sealed class Settings
{
    public string? ConnectionString { get; set; }
    public AppSettings? App { get; init; }
    public JwtSettings? Jwt { get; set; }
    public YoutubeSettings? Youtube { get; set; }
    public DeepSeekSettings? DeepSeek { get; set; }
    public AssistantSettings? Assistant { get; set; }

    public void Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(ConnectionString))
            errors.Add("ConnectionString is not configured.");

        if (App is null)
        {
            errors.Add("App section is not configured.");
        }
        else if (App.PreviewDurationSeconds <= 0)
        {
            errors.Add("App:PreviewDurationSeconds must be greater than 0.");
        }

        if (Jwt is null)
        {
            errors.Add("Jwt section is not configured.");
        }
        else
        {
            if (string.IsNullOrEmpty(Jwt.Key))
                errors.Add("Jwt:Key is not configured.");
            if (string.IsNullOrEmpty(Jwt.Issuer))
                errors.Add("Jwt:Issuer is not configured.");
            if (string.IsNullOrEmpty(Jwt.Audience))
                errors.Add("Jwt:Audience is not configured.");
        }

        if (Youtube is null)
        {
            errors.Add("Youtube section is not configured.");
        }
        else
        {
            if (string.IsNullOrEmpty(Youtube.ApiKey))
                errors.Add("Youtube:ApiKey is not configured.");
            if (string.IsNullOrEmpty(Youtube.YtDlpPath))
                errors.Add("Youtube:YtDlpPath is not configured.");
            if (string.IsNullOrEmpty(Youtube.FfmpegPath))
                errors.Add("Youtube:FfmpegPath is not configured.");
        }

        if (Assistant is { DailyMessageLimitPerUser: <= 0 })
            errors.Add("Assistant:DailyMessageLimitPerUser must be greater than 0.");

        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"Configuration validation failed:\n{string.Join("\n", errors)}");
    }

    public sealed class AppSettings
    {
        public string? BaseDirectory { get; set; }
        public int PreviewDurationSeconds { get; set; } = 10;
    }

    public sealed class JwtSettings
    {
        public string? Key { get; set; }
        public string? Issuer { get; set; }
        public string? Audience { get; set; }
    }

    public sealed class YoutubeSettings
    {
        public string? ApiKey { get; set; }
        public string? YtDlpPath { get; set; }
        public string? FfmpegPath { get; set; }
        public string? CookiesFile { get; set; }
        public string? CookiesFromBrowser { get; set; }
    }

    public sealed class DeepSeekSettings
    {
        public string? ApiKey { get; set; }
        public string? BaseUrl { get; set; } = "https://api.deepseek.com";
        public string? Model { get; set; } = "deepseek-chat";
        public int MaxIterations { get; set; } = 5;
        public int RequestTimeoutSeconds { get; set; } = 60;
    }

    public sealed class AssistantSettings
    {
        public int DailyMessageLimitPerUser { get; set; } = 30;
    }
}