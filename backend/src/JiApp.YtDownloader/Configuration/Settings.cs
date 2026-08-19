namespace JiApp.YtDownloader.Configuration;

public sealed class Settings
{
    public string? ConnectionString { get; set; }
    public AppSettings? App { get; init; }
    public JwtSettings? Jwt { get; set; }
    public string[]? CorsAllowedOrigins { get; set; }
    public YoutubeSettings? Youtube { get; set; }
    public DeepSeekSettings? DeepSeek { get; set; }
    public AssistantSettings? Assistant { get; set; }

    public void Validate(IWebHostEnvironment? env = null)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(ConnectionString))
            errors.Add("ConnectionString is not configured.");

        if (App is null)
        {
            errors.Add("App section is not configured.");
        }
        else
        {
            if (App.PreviewDurationSeconds <= 0)
                errors.Add("App:PreviewDurationSeconds must be greater than 0.");
            if (App.DownloadTtlMinutes <= 0)
                errors.Add("App:DownloadTtlMinutes must be greater than 0.");
            if (App.DownloadJobTimeoutMinutes <= 0)
                errors.Add("App:DownloadJobTimeoutMinutes must be greater than 0.");
            if (env?.IsProduction() == true && string.IsNullOrWhiteSpace(App.PublicBaseUrl))
                errors.Add("App:PublicBaseUrl is required in Production so download links use the public Gateway base URL, not the container hostname.");
            if (!string.IsNullOrWhiteSpace(App.PublicBaseUrl)
                && !Uri.TryCreate(App.PublicBaseUrl, UriKind.Absolute, out _))
                errors.Add("App:PublicBaseUrl must be a valid absolute URI.");
        }

        if (Jwt is null)
        {
            errors.Add("Jwt section is not configured.");
        }
        else
        {
            if (string.IsNullOrEmpty(Jwt.Key))
                errors.Add("Jwt:Key is not configured.");
            else if (Jwt.Key.Length < 32)
                errors.Add("Jwt:Key must be at least 32 characters long.");
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
            if (Youtube.MaxResults is null or <= 0)
                errors.Add("Youtube:MaxResults must be configured and greater than 0.");
            if (Youtube.PageSize is null or <= 0)
                errors.Add("Youtube:PageSize must be configured and greater than 0.");
            if (Youtube.PageSize.HasValue && Youtube.MaxResults.HasValue && Youtube.PageSize > Youtube.MaxResults)
                errors.Add("Youtube:PageSize must be less than or equal to Youtube:MaxResults.");
        }

        if (Assistant is { DailyMessageLimitPerUser: <= 0 })
            errors.Add("Assistant:DailyMessageLimitPerUser must be greater than 0.");
        if (Assistant is { MaxMessagesPerTurn: <= 0 })
            errors.Add("Assistant:MaxMessagesPerTurn must be greater than 0.");

        // DeepSeek is optional: an unset section (or empty ApiKey) passes. When the section
        // is present, validate the structural fields only — never require ApiKey.
        if (DeepSeek is not null)
        {
            if (DeepSeek.BaseUrl is not null && !Uri.TryCreate(DeepSeek.BaseUrl, UriKind.Absolute, out _))
                errors.Add("DeepSeek:BaseUrl must be a valid absolute URI.");
            if (DeepSeek.Model is { Length: > 0 } && string.IsNullOrWhiteSpace(DeepSeek.Model))
                errors.Add("DeepSeek:Model must not be empty or whitespace.");
            if (DeepSeek.MaxIterations <= 0)
                errors.Add("DeepSeek:MaxIterations must be greater than 0.");
            if (DeepSeek.RequestTimeoutSeconds <= 0)
                errors.Add("DeepSeek:RequestTimeoutSeconds must be greater than 0.");
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"Configuration validation failed:\n{string.Join("\n", errors)}");
    }

    public sealed class AppSettings
    {
        public string? BaseDirectory { get; set; }
        public int PreviewDurationSeconds { get; set; } = 10;
        public int DownloadTtlMinutes { get; set; } = 15;
        public int DownloadJobTimeoutMinutes { get; set; } = 5;
        public string? PublicBaseUrl { get; set; }
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
        public string? Proxy { get; set; }
        public int? MaxResults { get; set; }
        public int? PageSize { get; set; }

        public int ValidatedMaxResults => MaxResults ?? throw new InvalidOperationException("Youtube:MaxResults not configured after validation");
        public int ValidatedPageSize => PageSize ?? throw new InvalidOperationException("Youtube:PageSize not configured after validation");
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
        public int MaxMessagesPerTurn { get; set; } = 20;
    }
}