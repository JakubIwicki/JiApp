using System;
using System.Collections.Generic;
using JiApp.Api.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace JiApp.Tests.Configuration;

public class SettingsTests
{
    private static void AddRateLimitPolicy(Dictionary<string, string?> values, string name,
        int permitLimit = 10, int windowSeconds = 60, int queueLimit = 0, int segmentsPerWindow = 0)
    {
        values[$"RateLimiting:{name}:PermitLimit"] = permitLimit.ToString();
        values[$"RateLimiting:{name}:WindowInSeconds"] = windowSeconds.ToString();
        values[$"RateLimiting:{name}:QueueLimit"] = queueLimit.ToString();
        values[$"RateLimiting:{name}:SegmentsPerWindow"] = segmentsPerWindow.ToString();
    }

    private static Settings CreateSettings(Action<Dictionary<string, string?>> configure)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionString"] = "Data Source=test.db",
            ["Jwt:Key"] = "test-key-32-chars-minimum-for-hmac-sha256!",
            ["Jwt:Issuer"] = "TestIssuer",
            ["Jwt:Audience"] = "TestAudience",
            ["Jwt:ExpireMinutes"] = "30",
            ["Youtube:ApiKey"] = "test-api-key",
            ["Youtube:YtDlpPath"] = "yt-dlp",
            ["Youtube:FfmpegPath"] = "ffmpeg",
            ["App:BaseDirectory"] = "/test",
        };
        AddRateLimitPolicy(values, "Login", permitLimit: 5);
        AddRateLimitPolicy(values, "Register", permitLimit: 3);
        AddRateLimitPolicy(values, "Health", permitLimit: 30);
        AddRateLimitPolicy(values, "DownloadFile", segmentsPerWindow: 4);
        AddRateLimitPolicy(values, "SearchVideos", permitLimit: 30, segmentsPerWindow: 4);
        AddRateLimitPolicy(values, "SearchHistory", permitLimit: 20);
        AddRateLimitPolicy(values, "DownloadHistory", permitLimit: 20);
        AddRateLimitPolicy(values, "GetHistory", permitLimit: 20);
        AddRateLimitPolicy(values, "Me", permitLimit: 60);
        AddRateLimitPolicy(values, "GetDownloadLink", segmentsPerWindow: 4);
        configure(values);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var settings = new Settings();
        config.Bind(settings);
        return settings;
    }

    [Fact]
    public void Validate_AllFieldsSet_DoesNotThrow()
    {
        var settings = CreateSettings(_ => { });

        Action act = settings.Validate;

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_MissingConnectionString_ThrowsWithMessage()
    {
        var settings = CreateSettings(d => d.Remove("ConnectionString"));

        Action act = settings.Validate;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionString is not configured*");
    }

    [Fact]
    public void Validate_MissingJwtKey_ThrowsWithMessage()
    {
        var settings = CreateSettings(d => d.Remove("Jwt:Key"));

        Action act = settings.Validate;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Key is not configured*");
    }

    [Fact]
    public void Validate_MissingJwtIssuer_ThrowsWithMessage()
    {
        var settings = CreateSettings(d => d.Remove("Jwt:Issuer"));

        Action act = settings.Validate;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Issuer is not configured*");
    }

    [Fact]
    public void Validate_MissingJwtAudience_ThrowsWithMessage()
    {
        var settings = CreateSettings(d => d.Remove("Jwt:Audience"));

        Action act = settings.Validate;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Audience is not configured*");
    }

    [Fact]
    public void Validate_MissingJwtExpireMinutes_ThrowsWithMessage()
    {
        var settings = CreateSettings(d => d.Remove("Jwt:ExpireMinutes"));

        Action act = settings.Validate;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ExpireMinutes is not configured*");
    }

    [Fact]
    public void Validate_MissingYoutubeApiKey_ThrowsWithMessage()
    {
        var settings = CreateSettings(d => d.Remove("Youtube:ApiKey"));

        Action act = settings.Validate;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Youtube:ApiKey is not configured*");
    }

    [Fact]
    public void Validate_MissingYtDlpPath_ThrowsWithMessage()
    {
        var settings = CreateSettings(d => d.Remove("Youtube:YtDlpPath"));

        Action act = settings.Validate;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Youtube:YtDlpPath is not configured*");
    }

    [Fact]
    public void Validate_MissingFfmpegPath_ThrowsWithMessage()
    {
        var settings = CreateSettings(d => d.Remove("Youtube:FfmpegPath"));

        Action act = settings.Validate;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Youtube:FfmpegPath is not configured*");
    }

    [Fact]
    public void Validate_MissingAppSection_ThrowsWithMessage()
    {
        var settings = CreateSettings(d => d.Remove("App:BaseDirectory"));

        Action act = settings.Validate;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*App section*");
    }

    [Fact]
    public void Validate_MissingRateLimitingSection_ThrowsWithMessage()
    {
        var settings = CreateSettings(d => d.Remove("RateLimiting:Login:PermitLimit"));

        Action act = settings.Validate;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RateLimiting*");
    }

    [Fact]
    public void Validate_JwtExpireMinutesNonPositive_ThrowsWithMessage()
    {
        var settings = CreateSettings(d => d["Jwt:ExpireMinutes"] = "0");

        Action act = settings.Validate;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ExpireMinutes must be greater than 0*");
    }

    [Fact]
    public void Validate_JwtExpireMinutesNegative_ThrowsWithMessage()
    {
        var settings = CreateSettings(d => d["Jwt:ExpireMinutes"] = "-5");

        Action act = settings.Validate;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ExpireMinutes must be greater than 0*");
    }

    [Fact]
    public void Bind_PopulatesTopLevelConnectionString()
    {
        var settings = CreateSettings(_ => { });

        settings.ConnectionString.Should().Be("Data Source=test.db");
    }

    [Fact]
    public void Bind_PopulatesNestedYoutubeSettings()
    {
        var settings = CreateSettings(_ => { });

        settings.Youtube.Should().NotBeNull();
        settings.Youtube!.ApiKey.Should().Be("test-api-key");
        settings.Youtube!.YtDlpPath.Should().Be("yt-dlp");
        settings.Youtube!.FfmpegPath.Should().Be("ffmpeg");
    }

    [Fact]
    public void Bind_PopulatesNestedJwtSettings()
    {
        var settings = CreateSettings(_ => { });

        settings.Jwt.Should().NotBeNull();
        settings.Jwt!.Key.Should().Be("test-key-32-chars-minimum-for-hmac-sha256!");
        settings.Jwt!.Issuer.Should().Be("TestIssuer");
        settings.Jwt!.Audience.Should().Be("TestAudience");
        settings.Jwt!.ExpireMinutes.Should().Be(30);
    }

    [Fact]
    public void Bind_NullWhenMissing()
    {
        var config = new ConfigurationBuilder().Build();
        var settings = new Settings();
        config.Bind(settings);

        settings.ConnectionString.Should().BeNull();
        settings.Jwt.Should().BeNull();
        settings.Youtube.Should().BeNull();
        settings.App.Should().BeNull();
        settings.RateLimiting.Should().BeNull();
    }

    [Fact]
    public void Validate_AllFieldsMissing_ThrowsWithAllErrors()
    {
        var config = new ConfigurationBuilder().Build();
        var settings = new Settings();
        config.Bind(settings);

        Action act = settings.Validate;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionString*")
            .WithMessage("*App section*")
            .WithMessage("*Jwt section*")
            .WithMessage("*Youtube section*")
            .WithMessage("*RateLimiting section*");
    }
}