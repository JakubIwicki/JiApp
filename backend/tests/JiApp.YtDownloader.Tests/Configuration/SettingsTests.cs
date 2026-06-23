using JiApp.YtDownloader.Configuration;

namespace JiApp.YtDownloader.Tests.Configuration;

public sealed class SettingsTests
{
    private Settings CreateValidSettings(int previewDurationSeconds = 10)
    {
        return new Settings
        {
            ConnectionString = "Data Source=test.db",
            App = new Settings.AppSettings
            {
                BaseDirectory = "/tmp",
                PreviewDurationSeconds = previewDurationSeconds,
            },
            Jwt = new Settings.JwtSettings
            {
                Key = "test-key", Issuer = "test-issuer", Audience = "test-audience",
            },
            Youtube = new Settings.YoutubeSettings
            {
                ApiKey = "test-key", YtDlpPath = "yt-dlp", FfmpegPath = "ffmpeg",
            },
        };
    }

    [Fact]
    public void AppSettings_PreviewDurationSeconds_DefaultsTo10()
    {
        var app = new Settings.AppSettings();

        app.PreviewDurationSeconds.Should().Be(10);
    }

    [Fact]
    public void AppSettings_PreviewDurationSeconds_CanBeConfigured()
    {
        var app = new Settings.AppSettings { PreviewDurationSeconds = 30 };

        app.PreviewDurationSeconds.Should().Be(30);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_Throws_WhenPreviewDurationSecondsIsNotPositive(int invalidDuration)
    {
        var settings = CreateValidSettings(previewDurationSeconds: invalidDuration);

        Action act = () => settings.Validate();

        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("PreviewDurationSeconds");
    }

    [Fact]
    public void Validate_Passes_WhenPreviewDurationSecondsIsPositive()
    {
        var settings = CreateValidSettings(previewDurationSeconds: 15);

        Action act = () => settings.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void YoutubeSettings_CookiesFile_DefaultsToNull()
    {
        var youtube = new Settings.YoutubeSettings();

        youtube.CookiesFile.Should().BeNull();
    }

    [Fact]
    public void YoutubeSettings_CookiesFromBrowser_DefaultsToNull()
    {
        var youtube = new Settings.YoutubeSettings();

        youtube.CookiesFromBrowser.Should().BeNull();
    }

    [Fact]
    public void YoutubeSettings_CookieProperties_CanBeConfigured()
    {
        var youtube = new Settings.YoutubeSettings
        {
            CookiesFile = "/app/cookies.txt",
            CookiesFromBrowser = "firefox",
        };

        youtube.CookiesFile.Should().Be("/app/cookies.txt");
        youtube.CookiesFromBrowser.Should().Be("firefox");
    }

    [Fact]
    public void Validate_Passes_WhenCookiePropertiesAreNotSet()
    {
        var settings = new Settings
        {
            ConnectionString = "Data Source=test.db",
            App = new Settings.AppSettings
            {
                BaseDirectory = "/tmp",
                PreviewDurationSeconds = 10,
            },
            Jwt = new Settings.JwtSettings
            {
                Key = "test-key", Issuer = "test-issuer", Audience = "test-audience",
            },
            Youtube = new Settings.YoutubeSettings
            {
                ApiKey = "test-key",
                YtDlpPath = "yt-dlp",
                FfmpegPath = "ffmpeg",
            },
        };

        Action act = () => settings.Validate();

        act.Should().NotThrow();
    }

    // ── DeepSeek + Assistant ───────────────────────────────────────────────

    [Fact]
    public void DeepSeekSettings_defaults_are_set()
    {
        var deepSeek = new Settings.DeepSeekSettings();

        deepSeek.ApiKey.Should().BeNull();
        deepSeek.BaseUrl.Should().Be("https://api.deepseek.com");
        deepSeek.Model.Should().Be("deepseek-chat");
        deepSeek.MaxIterations.Should().Be(5);
        deepSeek.RequestTimeoutSeconds.Should().Be(60);
    }

    [Fact]
    public void AssistantSettings_DailyMessageLimitPerUser_defaults_to_30()
    {
        var assistant = new Settings.AssistantSettings();

        assistant.DailyMessageLimitPerUser.Should().Be(30);
    }

    [Fact]
    public void Validate_passes_when_DeepSeek_is_null()
    {
        var settings = ValidSettings();
        settings.DeepSeek = null;

        Action act = () => settings.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_passes_when_DeepSeek_ApiKey_is_empty()
    {
        var settings = ValidSettings();
        settings.DeepSeek = new Settings.DeepSeekSettings { ApiKey = "" };

        Action act = () => settings.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_passes_when_Assistant_is_null()
    {
        var settings = ValidSettings();
        settings.Assistant = null;

        Action act = () => settings.Validate();

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_throws_when_Assistant_DailyMessageLimitPerUser_is_not_positive(int invalidLimit)
    {
        var settings = ValidSettings();
        settings.Assistant = new Settings.AssistantSettings { DailyMessageLimitPerUser = invalidLimit };

        Action act = () => settings.Validate();

        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("DailyMessageLimitPerUser");
    }

    [Fact]
    public void Validate_passes_when_Assistant_DailyMessageLimitPerUser_is_positive()
    {
        var settings = ValidSettings();
        settings.Assistant = new Settings.AssistantSettings { DailyMessageLimitPerUser = 50 };

        Action act = () => settings.Validate();

        act.Should().NotThrow();
    }

    private static Settings ValidSettings() => new()
    {
        ConnectionString = "Data Source=test.db",
        App = new Settings.AppSettings { BaseDirectory = "/tmp", PreviewDurationSeconds = 10 },
        Jwt = new Settings.JwtSettings
        {
            Key = "test-key", Issuer = "test-issuer", Audience = "test-audience",
        },
        Youtube = new Settings.YoutubeSettings
        {
            ApiKey = "test-key", YtDlpPath = "yt-dlp", FfmpegPath = "ffmpeg",
        },
    };
}