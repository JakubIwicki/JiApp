using JiApp.Common.Authentication;
using JiApp.Scheduler.Configuration;
using Microsoft.AspNetCore.Hosting;

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
            Jwt = new JwtSettings { Key = "test-jwt-key-with-at-least-32-chars", Issuer = "iss", Audience = "aud" }
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
            Jwt = new JwtSettings { Key = "test-jwt-key-with-at-least-32-chars", Issuer = "iss", Audience = "aud" }
        };

        var act = () => settings.Validate(CreateEnvironment("Development"));

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithProductionEnvironment_MissingCorsAndIdentityBaseUrl_ThrowsListingBoth()
    {
        var settings = new SchedulerSettings
        {
            ConnectionString = "Data Source=test.db",
            Jwt = new JwtSettings { Key = "test-jwt-key-with-at-least-32-chars", Issuer = "iss", Audience = "aud" }
        };

        var act = () => settings.Validate(CreateEnvironment("Production"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*CorsAllowedOrigins*")
            .WithMessage("*IdentityBaseUrl*");
    }

    [Fact]
    public void Validate_WithDevelopmentEnvironment_MissingCorsAndIdentityBaseUrl_DoesNotThrow()
    {
        var settings = new SchedulerSettings
        {
            ConnectionString = "Data Source=test.db",
            Jwt = new JwtSettings { Key = "test-jwt-key-with-at-least-32-chars", Issuer = "iss", Audience = "aud" }
        };

        var act = () => settings.Validate(CreateEnvironment("Development"));

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithProductionEnvironment_ValidCorsAndIdentityBaseUrl_DoesNotThrow()
    {
        var settings = new SchedulerSettings
        {
            ConnectionString = "Data Source=test.db",
            Jwt = new JwtSettings { Key = "test-jwt-key-with-at-least-32-chars", Issuer = "iss", Audience = "aud" },
            CorsAllowedOrigins = ["https://app.example.com"],
            IdentityBaseUrl = "https://identity.example.com"
        };

        var act = () => settings.Validate(CreateEnvironment("Production"));

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithProductionEnvironment_WildcardCorsOrigin_DoesNotThrow()
    {
        var settings = new SchedulerSettings
        {
            ConnectionString = "Data Source=test.db",
            Jwt = new JwtSettings { Key = "test-jwt-key-with-at-least-32-chars", Issuer = "iss", Audience = "aud" },
            CorsAllowedOrigins = ["*"],
            IdentityBaseUrl = "https://identity.example.com"
        };

        var act = () => settings.Validate(CreateEnvironment("Production"));

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithProductionEnvironment_PathBearingCorsOrigin_Throws()
    {
        var settings = new SchedulerSettings
        {
            ConnectionString = "Data Source=test.db",
            Jwt = new JwtSettings { Key = "test-jwt-key-with-at-least-32-chars", Issuer = "iss", Audience = "aud" },
            CorsAllowedOrigins = ["https://app.example.com/api"],
            IdentityBaseUrl = "https://identity.example.com"
        };

        var act = () => settings.Validate(CreateEnvironment("Production"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not a valid http(s) origin*");
    }

    [Fact]
    public void Validate_WithProductionEnvironment_InvalidCorsOrigin_Throws()
    {
        var settings = new SchedulerSettings
        {
            ConnectionString = "Data Source=test.db",
            Jwt = new JwtSettings { Key = "test-jwt-key-with-at-least-32-chars", Issuer = "iss", Audience = "aud" },
            CorsAllowedOrigins = ["not-a-url"],
            IdentityBaseUrl = "https://identity.example.com"
        };

        var act = () => settings.Validate(CreateEnvironment("Production"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not-a-url*");
    }

    [Fact]
    public void Validate_WithProductionEnvironment_InvalidIdentityBaseUrl_Throws()
    {
        var settings = new SchedulerSettings
        {
            ConnectionString = "Data Source=test.db",
            Jwt = new JwtSettings { Key = "test-jwt-key-with-at-least-32-chars", Issuer = "iss", Audience = "aud" },
            CorsAllowedOrigins = ["https://app.example.com"],
            IdentityBaseUrl = "not-a-url"
        };

        var act = () => settings.Validate(CreateEnvironment("Production"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*IdentityBaseUrl must be a valid absolute URI*");
    }

    [Fact]
    public void Validate_AccumulatesConnectionStringAndJwtErrors_InOneCall()
    {
        var settings = new SchedulerSettings
        {
            ConnectionString = null,
            Jwt = new JwtSettings { Key = null, Issuer = null, Audience = null }
        };

        var act = () => settings.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionString*")
            .WithMessage("*Jwt:Key is not configured.*")
            .WithMessage("*Jwt:Issuer is not configured.*")
            .WithMessage("*Jwt:Audience is not configured.*");
    }

    [Theory]
    [InlineData(null, 50)]
    [InlineData(1000000, 100)]
    [InlineData(1, 1)]
    [InlineData(0, 1)]
    public void ClampTake_ReturnsValueClampedToPageBounds(int? take, int expected)
    {
        var settings = new SchedulerSettings();

        var result = settings.ClampTake(take);

        result.Should().Be(expected);
    }

    private static IWebHostEnvironment CreateEnvironment(string name)
    {
        var mock = new Mock<IWebHostEnvironment>();
        mock.SetupGet(e => e.EnvironmentName).Returns(name);
        return mock.Object;
    }
}
