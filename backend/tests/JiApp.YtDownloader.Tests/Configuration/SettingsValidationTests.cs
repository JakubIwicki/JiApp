using JiApp.YtDownloader.Configuration;
using Microsoft.AspNetCore.Hosting;
using Moq;

namespace JiApp.YtDownloader.Tests.Configuration;

public sealed class SettingsValidationTests
{
    private const string ValidPublicBaseUrl = "https://downloads.example.com";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Throws_WhenProductionAndPublicBaseUrlIsEmpty(string? publicBaseUrl)
    {
        var settings = ValidSettings();
        settings.App!.PublicBaseUrl = publicBaseUrl;

        Action act = () => settings.Validate(CreateEnvironment("Production"));

        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("PublicBaseUrl");
    }

    [Fact]
    public void Passes_WhenProductionAndPublicBaseUrlIsValidAbsoluteUrl()
    {
        var settings = ValidSettings();
        settings.App!.PublicBaseUrl = ValidPublicBaseUrl;

        Action act = () => settings.Validate(CreateEnvironment("Production"));

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    public void Passes_WhenNonProductionAndPublicBaseUrlIsMissing(string environmentName)
    {
        var settings = ValidSettings();
        settings.App!.PublicBaseUrl = null;

        Action act = () => settings.Validate(CreateEnvironment(environmentName));

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ht tp://host")]
    public void Throws_WhenPublicBaseUrlIsNotAnAbsoluteUri(string malformedUrl)
    {
        var settings = ValidSettings();
        settings.App!.PublicBaseUrl = malformedUrl;

        Action act = () => settings.Validate(CreateEnvironment("Development"));

        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("PublicBaseUrl");
    }

    private static Settings ValidSettings() => new()
    {
        ConnectionString = "Data Source=test.db",
        App = new Settings.AppSettings { BaseDirectory = "/tmp", PreviewDurationSeconds = 10 },
        Jwt = new Settings.JwtSettings
        {
            Key = "test-jwt-key-with-at-least-32-chars",
            Issuer = "test-issuer",
            Audience = "test-audience",
        },
        Youtube = new Settings.YoutubeSettings
        {
            ApiKey = "test-key",
            YtDlpPath = "yt-dlp",
            FfmpegPath = "ffmpeg",
            MaxResults = 30,
            PageSize = 10,
        },
    };

    private static IWebHostEnvironment CreateEnvironment(string name)
    {
        var mock = new Mock<IWebHostEnvironment>();
        mock.SetupGet(e => e.EnvironmentName).Returns(name);
        return mock.Object;
    }
}
