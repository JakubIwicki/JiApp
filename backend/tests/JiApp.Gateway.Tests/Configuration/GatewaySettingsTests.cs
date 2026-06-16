using JiApp.Gateway.Configuration;

namespace JiApp.Gateway.Tests.Configuration;

public sealed class GatewaySettingsTests
{
    [Fact]
    public void Validate_throws_when_Jwt_is_null()
    {
        var sut = new GatewaySettings();

        var act = () => sut.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt section is not configured.*");
    }

    [Fact]
    public void Validate_throws_when_Jwt_Key_is_empty()
    {
        var sut = new GatewaySettings
        {
            Jwt = new GatewaySettings.JwtSettings
            {
                Key = string.Empty,
                Issuer = "test-issuer",
                Audience = "test-audience"
            },
            RateLimiting = CreateValidPolicies()
        };

        var act = () => sut.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Key is not configured.*");
    }

    [Fact]
    public void Validate_throws_when_Jwt_Issuer_is_empty()
    {
        var sut = new GatewaySettings
        {
            Jwt = new GatewaySettings.JwtSettings
            {
                Key = "test-key-min-32-chars-!!!!!!!!!!!!!!!!",
                Issuer = string.Empty,
                Audience = "test-audience"
            },
            RateLimiting = CreateValidPolicies()
        };

        var act = () => sut.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Issuer is not configured.*");
    }

    [Fact]
    public void Validate_throws_when_Jwt_Audience_is_empty()
    {
        var sut = new GatewaySettings
        {
            Jwt = new GatewaySettings.JwtSettings
            {
                Key = "test-key-min-32-chars-!!!!!!!!!!!!!!!!",
                Issuer = "test-issuer",
                Audience = string.Empty
            },
            RateLimiting = CreateValidPolicies()
        };

        var act = () => sut.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Audience is not configured.*");
    }

    [Fact]
    public void Validate_throws_when_RateLimiting_is_null()
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
    public void Validate_throws_when_RateLimiting_is_empty()
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
    public void Validate_throws_when_policy_is_missing()
    {
        var sut = new GatewaySettings
        {
            Jwt = CreateValidJwt(),
            RateLimiting = new Dictionary<string, GatewaySettings.RateLimitPolicyConfig>
            {
                ["Login"] = CreateValidPolicy(),
                ["Register"] = CreateValidPolicy()
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
            .WithMessage("*RateLimiting:Scheduler is not configured.*")
            .WithMessage("*RateLimiting:Throw is not configured.*");
    }

    [Fact]
    public void Validate_passes_when_all_configured()
    {
        var sut = new GatewaySettings
        {
            Jwt = CreateValidJwt(),
            RateLimiting = CreateValidPolicies()
        };

        var act = () => sut.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_collects_all_Jwt_errors_simultaneously()
    {
        var sut = new GatewaySettings
        {
            Jwt = new GatewaySettings.JwtSettings
            {
                Key = string.Empty,
                Issuer = string.Empty,
                Audience = string.Empty
            },
            RateLimiting = CreateValidPolicies()
        };

        var act = () => sut.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Key is not configured.*")
            .WithMessage("*Jwt:Issuer is not configured.*")
            .WithMessage("*Jwt:Audience is not configured.*");
    }

    private static GatewaySettings.JwtSettings CreateValidJwt() => new()
    {
        Key = "test-key-min-32-chars-!!!!!!!!!!!!!!!!",
        Issuer = "test-issuer",
        Audience = "test-audience"
    };

    private static GatewaySettings.RateLimitPolicyConfig CreateValidPolicy() => new()
    {
        PermitLimit = 10,
        WindowInSeconds = 60,
        QueueLimit = 0,
        SegmentsPerWindow = 1
    };

    private static Dictionary<string, GatewaySettings.RateLimitPolicyConfig> CreateValidPolicies()
    {
        var policies = new[]
        {
            "Login", "Register", "Refresh", "Logout", "Health", "DownloadFile",
            "SearchVideos", "SearchHistory", "DownloadHistory", "GetHistory",
            "Me", "GetDownloadLink", "Preview", "Scheduler", "Throw"
        };

        return policies.ToDictionary(p => p, _ => CreateValidPolicy());
    }
}
