using JiApp.Common.Authentication;
using JiApp.Identity.Configuration;

namespace JiApp.Identity.Tests.Configuration;

public sealed class IdentitySettingsTests
{
    private static IdentitySettings.RateLimitPolicyConfig ValidPolicy => new()
    {
        PermitLimit = 10,
        WindowInSeconds = 60,
        QueueLimit = 0,
        SegmentsPerWindow = 1
    };

    [Fact]
    public void Validate_WithValidSettings_DoesNotThrow()
    {
        var settings = ValidSettings();

        var act = () => settings.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithWhitespaceBootstrapAdminUsername_ThrowsInvalidOperationException()
    {
        var settings = ValidSettings();
        settings.Bootstrap = new IdentitySettings.BootstrapSettings { AdminUsername = "   " };

        var act = () => settings.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Bootstrap:AdminUsername*");
    }

    [Fact]
    public void Validate_WithOverLengthBootstrapAdminUsername_ThrowsInvalidOperationException()
    {
        var settings = ValidSettings();
        settings.Bootstrap = new IdentitySettings.BootstrapSettings { AdminUsername = new string('a', 257) };

        var act = () => settings.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Bootstrap:AdminUsername*");
    }

    [Fact]
    public void Validate_WithEmptyBootstrapAdminUsername_DoesNotThrow()
    {
        var settings = ValidSettings();
        settings.Bootstrap = new IdentitySettings.BootstrapSettings { AdminUsername = "" };

        var act = () => settings.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_AccumulatesUnrelatedErrors_InOneCall()
    {
        var settings = new IdentitySettings();

        var act = () => settings.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionString is not configured.*")
            .WithMessage("*Jwt section is not configured.*")
            .WithMessage("*RateLimiting section is not configured.*");
    }

    private static IdentitySettings ValidSettings() => new()
    {
        ConnectionString = "Data Source=test.db",
        Jwt = new JwtSettings
        {
            Key = "test-jwt-key-with-at-least-32-chars",
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenExpireMinutes = 60,
            RefreshTokenExpireDays = 7
        },
        RateLimiting = new Dictionary<string, IdentitySettings.RateLimitPolicyConfig>
        {
            ["Login"] = ValidPolicy,
            ["Register"] = ValidPolicy,
            ["Refresh"] = ValidPolicy,
            ["Logout"] = ValidPolicy
        }
    };
}
