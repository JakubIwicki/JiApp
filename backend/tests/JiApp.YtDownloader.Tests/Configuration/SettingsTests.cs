using JiApp.YtDownloader.Configuration;

namespace JiApp.YtDownloader.Tests.Configuration;

public sealed class SettingsTests
{
    private sealed class Fixture
    {
        public static Fixture Init() => new();

        public Settings CreateValidSettings(int previewDurationSeconds = 10)
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
        var settings = new Fixture().CreateValidSettings(previewDurationSeconds: invalidDuration);

        Action act = () => settings.Validate();

        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("PreviewDurationSeconds");
    }

    [Fact]
    public void Validate_Passes_WhenPreviewDurationSecondsIsPositive()
    {
        var settings = new Fixture().CreateValidSettings(previewDurationSeconds: 15);

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
}