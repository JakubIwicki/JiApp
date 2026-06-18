using JiApp.Scheduler.Configuration;

namespace JiApp.Scheduler.Tests.Configuration;

public sealed class SchedulerSettingsTests
{
    [Fact]
    public void Validate_WithNullJwt_ThrowsInvalidOperationException()
    {
        var settings = new SchedulerSettings
        {
            ConnectionString = "Data Source=test.db",
            Jwt = null
        };

        var act = () => settings.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt*");
    }

    [Fact]
    public void Validate_WithNullConnectionString_ThrowsInvalidOperationException()
    {
        var settings = new SchedulerSettings
        {
            ConnectionString = null,
            Jwt = new JwtSettings { Key = "key", Issuer = "iss", Audience = "aud" }
        };

        var act = () => settings.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionString*");
    }

    [Fact]
    public void Validate_WithMissingJwtKey_ThrowsInvalidOperationException()
    {
        var settings = new SchedulerSettings
        {
            ConnectionString = "Data Source=test.db",
            Jwt = new JwtSettings { Key = null, Issuer = "iss", Audience = "aud" }
        };

        var act = () => settings.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Key*");
    }

    [Fact]
    public void Validate_WithValidSettings_DoesNotThrow()
    {
        var settings = new SchedulerSettings
        {
            ConnectionString = "Data Source=test.db",
            Jwt = new JwtSettings { Key = "key", Issuer = "iss", Audience = "aud" }
        };

        var act = () => settings.Validate();

        act.Should().NotThrow();
    }
}
