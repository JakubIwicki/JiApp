using JiApp.YtDownloader.Configuration;

namespace JiApp.YtDownloader.Tests.Configuration;

public class SettingsTests
{
    [Fact]
    public void AppSettings_PreviewDurationSeconds_defaults_to_10()
    {
        var app = new Settings.AppSettings();

        app.PreviewDurationSeconds.Should().Be(10);
    }

    [Fact]
    public void AppSettings_PreviewDurationSeconds_can_be_configured()
    {
        var app = new Settings.AppSettings { PreviewDurationSeconds = 30 };

        app.PreviewDurationSeconds.Should().Be(30);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_throws_when_PreviewDurationSeconds_is_not_positive(int invalidDuration)
    {
        var settings = new Settings
        {
            ConnectionString = "Data Source=test.db",
            App = new Settings.AppSettings
            {
                BaseDirectory = "/tmp",
                PreviewDurationSeconds = invalidDuration,
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

        Action act = () => settings.Validate();

        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("PreviewDurationSeconds");
    }

    [Fact]
    public void Validate_passes_when_PreviewDurationSeconds_is_positive()
    {
        var settings = new Settings
        {
            ConnectionString = "Data Source=test.db",
            App = new Settings.AppSettings
            {
                BaseDirectory = "/tmp",
                PreviewDurationSeconds = 15,
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

        Action act = () => settings.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void YoutubeSettings_CookiesFile_defaults_to_null()
    {
        var youtube = new Settings.YoutubeSettings();

        youtube.CookiesFile.Should().BeNull();
    }

    [Fact]
    public void YoutubeSettings_CookiesFromBrowser_defaults_to_null()
    {
        var youtube = new Settings.YoutubeSettings();

        youtube.CookiesFromBrowser.Should().BeNull();
    }

    [Fact]
    public void YoutubeSettings_cookie_properties_can_be_configured()
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
    public void Validate_passes_when_cookie_properties_are_not_set()
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
                // CookiesFile and CookiesFromBrowser left null — should still pass
            },
        };

        Action act = () => settings.Validate();

        act.Should().NotThrow();
    }
}