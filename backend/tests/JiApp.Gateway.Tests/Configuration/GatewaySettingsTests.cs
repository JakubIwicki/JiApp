using JiApp.Gateway.Configuration;

namespace JiApp.Gateway.Tests.Configuration;

public sealed class GatewaySettingsTests
{
    private sealed class Fixture
    {
        public static Fixture Init() => new();

        public static GatewaySettings.JwtSettings ValidJwt => new()
        {
            Key = "test-key-min-32-chars-!!!!!!!!!!!!!!!!",
            Issuer = "test-issuer",
            Audience = "test-audience"
        };

        public static GatewaySettings.RateLimitPolicyConfig ValidPolicy => new()
        {
            PermitLimit = 10,
            WindowInSeconds = 60,
            QueueLimit = 0,
            SegmentsPerWindow = 1
        };

        public static Dictionary<string, GatewaySettings.RateLimitPolicyConfig> ValidPolicies
        {
            get
            {
                var policies = new[]
                {
                    "Login", "Register", "Refresh", "Logout", "Health", "DownloadFile",
                    "SearchVideos", "SearchHistory", "DownloadHistory", "GetHistory",
                    "Me", "GetDownloadLink", "Preview", "Scheduler"
                };

                return policies.ToDictionary(p => p, _ => ValidPolicy);
            }
        }
    }

    [Fact]
    public void Validate_Throws_WhenJwtIsNull()
    {
        var sut = new GatewaySettings();

        var act = () => sut.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt section is not configured.*");
    }

    [Fact]
    public void Validate_Throws_WhenJwtKeyIsEmpty()
    {
        var sut = new GatewaySettings
        {
            Jwt = new GatewaySettings.JwtSettings
            {
                Key = string.Empty,
                Issuer = "test-issuer",
                Audience = "test-audience"
            },
            RateLimiting = Fixture.ValidPolicies
        };

        var act = () => sut.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Key is not configured.*");
    }

    [Fact]
    public void Validate_Throws_WhenJwtIssuerIsEmpty()
    {
        var sut = new GatewaySettings
        {
            Jwt = new GatewaySettings.JwtSettings
            {
                Key = "test-key-min-32-chars-!!!!!!!!!!!!!!!!",
                Issuer = string.Empty,
                Audience = "test-audience"
            },
            RateLimiting = Fixture.ValidPolicies
        };

        var act = () => sut.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Issuer is not configured.*");
    }

    [Fact]
    public void Validate_Throws_WhenJwtAudienceIsEmpty()
    {
        var sut = new GatewaySettings
        {
            Jwt = new GatewaySettings.JwtSettings
            {
                Key = "test-key-min-32-chars-!!!!!!!!!!!!!!!!",
                Issuer = "test-issuer",
                Audience = string.Empty
            },
            RateLimiting = Fixture.ValidPolicies
        };

        var act = () => sut.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Audience is not configured.*");
    }

    [Fact]
    public void Validate_Throws_WhenRateLimitingIsNull()
    {
        var sut = new GatewaySettings
        {
            Jwt = new GatewaySettings.JwtSettings
            {
                Key = "test-key-min-32-chars-!!!!!!!!!!!!!!!!",
                Issuer = "test-issuer",
                Audience = "test-audience"
            }
        };

        var act = () => sut.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RateLimiting section is not configured.*");
    }

    [Fact]
    public void Validate_Throws_WhenRateLimitingIsEmpty()
    {
        var sut = new GatewaySettings
        {
            Jwt = new GatewaySettings.JwtSettings
            {
                Key = "test-key-min-32-chars-!!!!!!!!!!!!!!!!",
                Issuer = "test-issuer",
                Audience = "test-audience"
            },
            RateLimiting = new Dictionary<string, GatewaySettings.RateLimitPolicyConfig>()
        };

        var act = () => sut.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RateLimiting section is not configured.*");
    }

    [Fact]
    public void Validate_Throws_WhenPolicyIsMissing()
    {
        var sut = new GatewaySettings
        {
            Jwt = Fixture.ValidJwt,
            RateLimiting = new Dictionary<string, GatewaySettings.RateLimitPolicyConfig>
            {
                ["Login"] = Fixture.ValidPolicy,
                ["Register"] = Fixture.ValidPolicy
            }
        };

        var act = () => sut.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RateLimiting:Refresh is not configured.*")
            .WithMessage("*RateLimiting:Logout is not configured.*")
            .WithMessage("*RateLimiting:Health is not configured.*")
            .WithMessage("*RateLimiting:DownloadFile is not configured.*")
            .WithMessage("*RateLimiting:SearchVideos is not configured.*")
            .WithMessage("*RateLimiting:SearchHistory is not configured.*")
            .WithMessage("*RateLimiting:DownloadHistory is not configured.*")
            .WithMessage("*RateLimiting:GetHistory is not configured.*")
            .WithMessage("*RateLimiting:Me is not configured.*")
            .WithMessage("*RateLimiting:GetDownloadLink is not configured.*")
            .WithMessage("*RateLimiting:Preview is not configured.*")
            .WithMessage("*RateLimiting:Scheduler is not configured.*");
    }

    [Fact]
    public void Validate_Passes_WhenAllConfigured()
    {
        var sut = new GatewaySettings
        {
            Jwt = Fixture.ValidJwt,
            RateLimiting = Fixture.ValidPolicies
        };

        var act = () => sut.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_CollectsAllJwtErrors_Simultaneously()
    {
        var sut = new GatewaySettings
        {
            Jwt = new GatewaySettings.JwtSettings
            {
                Key = string.Empty,
                Issuer = string.Empty,
                Audience = string.Empty
            },
            RateLimiting = Fixture.ValidPolicies
        };

        var act = () => sut.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Key is not configured.*")
            .WithMessage("*Jwt:Issuer is not configured.*")
            .WithMessage("*Jwt:Audience is not configured.*");
    }
}
